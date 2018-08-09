using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ModelManager.Types
{
	[Serializable]
	//Used to hold local settings for application
	public class LocalSettings
	{
		private static LocalSettings _instance;
		private static volatile object lockObject = new Object();

		private static string _repoPath;

		[XmlElement]
		public string RepoPath
		{
			get => _repoPath;
			set => _repoPath = value;
		}

		private static string _gitFilterFilePath;
		[XmlElement]
		public string GitFilterFilePath
		{
			get => _gitFilterFilePath;
			set => _gitFilterFilePath = value;
		}

        private static string _applicationPath;
        [XmlElement]
        public string ApplicationPath
        {
            get => _applicationPath;
            set => _applicationPath = value;
        }

        public static LocalSettings Instance()
		{
			var configFilePath = ConfigurationManager.AppSettings["LocalConfigFilePath"];

			if (_instance == null)
			{
				lock (lockObject)
				{
					if (_instance == null)
					{
						XmlSerializer serializer = XmlSerializer.FromTypes(new[] { typeof(LocalSettings) })[0];
                        if (!File.Exists(configFilePath))
                        {
                            var sw = new StreamWriter(configFilePath);
                            var appDirectory = AppDomain.CurrentDomain.BaseDirectory + @"..\..\..\";
                            serializer.Serialize(sw, new LocalSettings() { RepoPath = appDirectory, GitFilterFilePath = appDirectory + @"ModelManager\Local\RegexGitFilters.txt" } );
                            sw.Close();
                        }
						StreamReader reader = new StreamReader(configFilePath);
						    _instance = (LocalSettings)serializer.Deserialize(reader);
						reader.Close();
					}
				}
			}
			return _instance;
		}
	}
}
