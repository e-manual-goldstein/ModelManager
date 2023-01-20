using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace ModelManager.GitInteg.Sdk
{
    public class GitPush
    {

		public RefUpdate[] RefUpdates { get; set; }

		public class RefUpdate
		{
			public string Name { get; set; }
			public string OldObjectId { get; set; }
			public string NewObjectId { get; set; }
        }

		/*
         * 	"refUpdates": [
				{
					"repositoryId": "6d2f442a-0d61-4c4e-8cd8-b3b52f55fd79",
					"name": "refs/pull/52182/merge",
					"oldObjectId": "0000000000000000000000000000000000000000",
					"newObjectId": "a20517f4a7e77956999f636aa2ef509e6fa70e34"
				}
			],
		*/

		/*
			"repository": {
				"id": "6d2f442a-0d61-4c4e-8cd8-b3b52f55fd79",
				"name": "Trunk",
				"url": "http://vssdmlivetfs:8080/tfs/BOMiLiveTFS/24eb3e9c-e5fb-4987-9af6-abcabdc36e3f/_apis/git/repositories/6d2f442a-0d61-4c4e-8cd8-b3b52f55fd79",
				"project": {
					"id": "24eb3e9c-e5fb-4987-9af6-abcabdc36e3f",
					"name": "INTEG_MIRROR",
					"description": "Git Mirror of INTEG repository",
					"url": "http://vssdmlivetfs:8080/tfs/BOMiLiveTFS/_apis/projects/24eb3e9c-e5fb-4987-9af6-abcabdc36e3f",
					"state": "wellFormed",
					"revision": 12520610,
					"visibility": "private",
					"lastUpdateTime": "2022-08-18T01:01:51.137Z"
				},
				"size": 2449896042,
				"remoteUrl": "http://vssdmlivetfs:8080/tfs/BOMiLiveTFS/INTEG_MIRROR/_git/Trunk",
				"sshUrl": "ssh://vssdmlivetfs:22/tfs/BOMiLiveTFS/INTEG_MIRROR/_git/Trunk",
				"webUrl": "http://vssdmlivetfs:8080/tfs/BOMiLiveTFS/INTEG_MIRROR/_git/Trunk"
			},
		*/

		public Pusher PushedBy { get; set; }

		public class Pusher
		{
			public string DisplayName { get; set; }
		}

		public int PushId { get; set; }

		public DateTime Date { get; set; }
		/*
			"pushedBy": {
				"displayName": "Microsoft.TeamFoundation.System",
				"url": "http://vssdmlivetfs:8080/tfs/BOMiLiveTFS/_apis/Identities/000007f5-0000-8888-8000-000000000000",
				"_links": {
					"avatar": {
						"href": "http://vssdmlivetfs:8080/tfs/BOMiLiveTFS/_apis/GraphProfile/MemberAvatars/s2s.MDAwMDA3RjUtMDAwMC04ODg4LTgwMDAtMDAwMDAwMDAwMDAwQDAwMDAwMDAwLTAwMDAtMDAwMC0wMDAwLTAwMDAwMDAwMDAwMA"
					}
				},
				"id": "000007f5-0000-8888-8000-000000000000",
				"uniqueName": "000007F5-0000-8888-8000-000000000000@00000000-0000-0000-0000-000000000000",
				"imageUrl": "http://vssdmlivetfs:8080/tfs/BOMiLiveTFS/_api/_common/identityImage?id=000007f5-0000-8888-8000-000000000000",
				"descriptor": "s2s.MDAwMDA3RjUtMDAwMC04ODg4LTgwMDAtMDAwMDAwMDAwMDAwQDAwMDAwMDAwLTAwMDAtMDAwMC0wMDAwLTAwMDAwMDAwMDAwMA"
			},
			"pushId": 538216,
			"date": "2022-11-22T15:47:57.6590472Z",
			"url": "http://vssdmlivetfs:8080/tfs/BOMiLiveTFS/INTEG_MIRROR/_apis/git/repositories/6d2f442a-0d61-4c4e-8cd8-b3b52f55fd79/pushes/538216"
		},
         */
	}

	public class PushQueryResult 
	{
		public int Count { get; set; }

		public GitPush[] Value { get; set; }
	}

}
