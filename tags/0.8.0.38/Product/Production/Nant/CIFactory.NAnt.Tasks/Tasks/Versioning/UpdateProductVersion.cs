using System;
using System.IO;
using System.Collections;
using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Tasks;
using MattGriffith.UpdateVersion;

namespace UpdateVersion.Tasks.Tasks
{
	[TaskName("UpdateProductVersion")]
	public class UpdateProductVersion : Task
	{
		
		private string _File;
		private string _Version;

		[TaskAttribute("Version", Required=true)]
		public string Version
		{
			set
			{
				this._Version = value;
			}
			get
			{
				return this._Version;
			}
		}

		[TaskAttribute("File", Required=true)]
		public string File
		{
			set
			{
				this._File = value;
			}
			get
			{
				return this._File;
			}
		}

		protected override void ExecuteTask()
		{
			Options options = new Options();
			options.BuildNumberType = BuildNumberType.Fixed;
			options.InputFilename = this.File;
			options.OutputFilename = this.File;
			options.ReplacementVersion  = this.Version;
			options.AttributeName = "AssemblyInformationalVersion";
			RunUpdateVersion version = new RunUpdateVersion();
			version.execute(options);
		}

		public void test()
        {
            this.Version = "2.844.0.42";
            this.File = "C:\\Projects\\dod.ahlta\\Current\\Product\\ProjectInfo.vb";
			this.ExecuteTask();
		}

	}
}
