using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Xml;
using ThoughtWorks.CruiseControl.Core.Sourcecontrol;
using ThoughtWorks.CruiseControl.Core.Util;
using ThoughtWorks.CruiseControl.Remote;

namespace ThoughtWorks.CruiseControl.Core.Sourcecontrol
{
	/// <summary>
	/// Integrates with Vault 3.1.7 or later.
	/// </summary>
	public class Vault317 : Vault3
	{
		private long _folderVersion;
		private long _lastTxID;
		private CultureInfo culture = CultureInfo.CurrentCulture;

		public Vault317(VaultVersionChecker versionCheckerShim) : base(versionCheckerShim)
		{
			_folderVersion = 0;
			_lastTxID = 0;
		}

		/// <summary>
		/// Called only by the unit tests, sets up as appropriate.
		/// </summary>
		/// <param name="versionCheckerShim"></param>
		/// <param name="historyParser"></param>
		/// <param name="executor"></param>
		public Vault317(VaultVersionChecker versionCheckerShim, IHistoryParser historyParser, ProcessExecutor executor) : base(versionCheckerShim, historyParser, executor)
		{}

		public override Modification[] GetModifications(IIntegrationResult from, IIntegrationResult to)
		{
			if (LookForChangesUsingVersionHistory(from, to))
				return GetModificationsFromItemHistory(from, to);
			else
			{
				// This has to be an empty array, not null, when there are no changes.
				Modification[] mods = {};
				return mods;
			}
		}

		private Modification[] GetModificationsFromItemHistory(IIntegrationResult from, IIntegrationResult to)
		{
			Log.Info(string.Format("Retrieving detailed change list for {0} in Vault Repository \"{1}\" between {2} and {3}", _shim.Folder, _shim.Repository, from.StartTime, to.StartTime));
			ProcessResult result = ExecuteWithRetries(ForHistoryProcessInfo(from, to), to.ProjectName);
			Modification[] itemModifications = ParseModifications(result, from.StartTime, to.StartTime);
			if (itemModifications == null || itemModifications.Length == 0)
				Log.Warning("Item history returned no changes.  Version history is supposed to determine if changes exist.  This is usually caused by clock skew between the CC.NET server and the Vault server.");

			// Unfortunately we have to go through these one more time to ensure there's nothing beyond the version we're going to retrieve.
			// We've made two history queries, and if changes were committed after our first check it's possible that they would be erroneously
			// included in the list of mods without this extra check.
			ArrayList modList = new ArrayList(itemModifications.Length);
			foreach (Modification mod in itemModifications)
			{
				if (mod.ChangeNumber <= _lastTxID)
					modList.Add(mod);
			}

			return (Modification[]) modList.ToArray(typeof (Modification));
		}

		private bool LookForChangesUsingVersionHistory(IIntegrationResult from, IIntegrationResult to)
		{
			Log.Info(string.Format("Checking for modifications to {0} in Vault Repository \"{1}\" between {2} and {3}", _shim.Folder, _shim.Repository, from.StartTime, to.StartTime));

			bool bFoundChanges = GetFolderVersion(from, to);

			Log.Debug("The folder has" + (bFoundChanges ? " " : " not ") + "changed.");

			return bFoundChanges;
		}

		public override void GetSource(IIntegrationResult result)
		{
			if (!_shim.AutoGetSource) return;
			Debug.Assert(_folderVersion > 0, "_folderVersion <= 0 when attempting to get source.  This shouldn't happen.");

			if (_shim.CleanCopy)
			{
				string cleanCopyWorkingFolder = null;
				if (StringUtil.IsBlank(_shim.WorkingDirectory))
				{
					cleanCopyWorkingFolder = GetVaultWorkingFolder(result);
					if (StringUtil.IsBlank(cleanCopyWorkingFolder))
						throw new VaultException(
							string.Format("Vault user {0} has no working folder set for {1} in repository {2} and no working directory has been specified.",
							              _shim.Username, _shim.Folder, _shim.Repository));
				}
				else
					cleanCopyWorkingFolder = result.BaseFromWorkingDirectory(_shim.WorkingDirectory);

				Log.Debug("Cleaning out source folder: " + cleanCopyWorkingFolder);
				new IoService().EmptyDirectoryIncludingReadOnlyObjects(cleanCopyWorkingFolder);
			}

			Log.Info("Getting source from Vault");
			Execute(GetSourceProcessInfo(result), result.ProjectName);
		}

		public override void LabelSourceControl(IIntegrationResult result)
		{
			// only apply label if it's turned on and the integration was a success
			if (!_shim.ApplyLabel || result.Status != IntegrationStatus.Success) return;

			Debug.Assert(_folderVersion > 0, "_folderVersion <= 0 when attempting to label.  This shouldn't happen.");

			Log.Info(string.Format("Applying label \"{0}\" to version {1} of {2} in repository {3}.",
			                       result.Label, _folderVersion, _shim.Folder, _shim.Repository));
			Execute(LabelProcessInfo(result), result.ProjectName);
		}

		private ProcessInfo LabelProcessInfo(IIntegrationResult result)
		{
			ProcessArgumentBuilder builder = new ProcessArgumentBuilder();
			builder.AddArgument("label", _shim.Folder);
			builder.AddArgument(result.Label);
			builder.AddArgument(_folderVersion.ToString());
			AddCommonOptionalArguments(builder);
			return ProcessInfoFor(builder.ToString(), result);
		}

		/// <summary>
		/// Gets the most recent folder version via Vault's versionhistory command.
		/// 
		/// If we don't yet have a folder version, we need to get one so getSource and LabelSource have a version to work with,
		/// whether there's been changes or not.  (On a forced build or a multi-source control setup, we might get and/or label
		/// when there's been no change.)
		/// 
		/// So if we have no folder version, we get the latest version of the folder via Vault's versionhistory command and see
		/// if the timestamp on that folder is more recent than the last build.  If we already have a folder version, we simply
		/// ask Vault to give us the most recent folder version after then one we already know about.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <returns></returns>
		private bool GetFolderVersion(IIntegrationResult from, IIntegrationResult to)
		{
			bool bFoundChanges = false;

			// If we don't yet have a folder version, we need to just get the latest, rather than checking for a change.
			bool bForceGetLatestVersion = (_folderVersion == 0);

			// get version history
			ProcessResult result = ExecuteWithRetries(VersionHistoryProcessInfo(from, to, bForceGetLatestVersion), to.ProjectName);

			// parse out changes
			string versionHistory = Vault3.ExtractXmlFromOutput(result.StandardOutput);
			XmlDocument versionHistoryXml = new XmlDocument();
			versionHistoryXml.LoadXml(versionHistory);
			XmlNodeList versionNodeList = versionHistoryXml.SelectNodes("/vault/history/item");
			XmlNode folderVersionNode = null;
			if (bForceGetLatestVersion)
			{
				Debug.Assert(versionNodeList.Count == 1, "Attempted to retrieve folder's current version and got no results.");
				folderVersionNode = versionNodeList.Item(0);
			}
			else
			{
				Debug.Assert(versionNodeList.Count == 0 || versionNodeList.Count == 1, "Vault versionhistory -rowlimit 1 returned more than 1 row.");
				if (versionNodeList.Count == 1)
				{
					folderVersionNode = versionNodeList.Item(0);
					// We asked vault for only new folder versions, so if we got one, the folder has changed.
					bFoundChanges = true;
				}
			}

			if (folderVersionNode != null)
			{
				if (bForceGetLatestVersion)
				{
					// We asked Vault for the most recent folder version.  We have to check its date to
					// see if this represents a change since the last integration.
					XmlAttribute dateAttr = (XmlAttribute) folderVersionNode.Attributes.GetNamedItem("date");
					Debug.Assert(dateAttr != null, "date attribute not found in version history");
					DateTime dtLastChange = DateTime.Parse(dateAttr.Value, culture);
					if (dtLastChange > from.StartTime)
						bFoundChanges = true;
				}
				// get the new most recent folder version
				XmlAttribute versionAttr = (XmlAttribute) folderVersionNode.Attributes.GetNamedItem("version");
				Debug.Assert(versionAttr != null, "version attribute not found in version history");
				_folderVersion = long.Parse(versionAttr.Value);
				Log.Debug("Most recent folder version: " + _folderVersion);

				// get the new most recent TxId
				XmlAttribute txIdAttr = (XmlAttribute) folderVersionNode.Attributes.GetNamedItem("txid");
				Debug.Assert(txIdAttr != null, "txid attribute not found in version history");
				_lastTxID = long.Parse(txIdAttr.Value);
				Log.Debug("Most recent TxID: " + _lastTxID);
			}

			return bFoundChanges;
		}

		private ProcessInfo VersionHistoryProcessInfo(IIntegrationResult from, IIntegrationResult to, bool bForceGetLatestVersion)
		{
			ProcessArgumentBuilder builder = new ProcessArgumentBuilder();
			builder.AddArgument("versionhistory", _shim.Folder);

			// Look only for changes, unless caller asked us to get the latest folder 
			// version regardless of whether there's been a change.
			if (!bForceGetLatestVersion)
			{
				// use folderVersion when possible because it's faster and more accurate
				if (_folderVersion != 0)
				{
					builder.AddArgument("-beginversion", (_folderVersion + 1).ToString());
				}
				else
				{
					builder.AddArgument("-begindate", from.StartTime.ToString("s"));
					builder.AddArgument("-enddate", to.StartTime.ToString("s"));
				}
			}

			// we only ever need the most recent change
			builder.AddArgument("-rowlimit", "1");

			AddCommonOptionalArguments(builder);
			return ProcessInfoFor(builder.ToString(), from);
		}

		private ProcessInfo GetSourceProcessInfo(IIntegrationResult result)
		{
			ProcessArgumentBuilder builder = new ProcessArgumentBuilder();

			builder.AddArgument("getversion", _folderVersion.ToString());
			builder.AddArgument(_shim.Folder);

			if (!StringUtil.IsBlank(_shim.WorkingDirectory))
			{
				builder.AddArgument(result.BaseFromWorkingDirectory(_shim.WorkingDirectory));
				if (_shim.UseVaultWorkingDirectory)
					builder.AppendArgument("-useworkingfolder");
			}

			builder.AddArgument("-merge", "overwrite");
			builder.AppendArgument("-makewritable");
			builder.AddArgument("-backup", "no");
			builder.AddArgument("-setfiletime", _shim.setFileTime);
			AddCommonOptionalArguments(builder);
			return ProcessInfoFor(builder.ToString(), result);
		}

	}
}