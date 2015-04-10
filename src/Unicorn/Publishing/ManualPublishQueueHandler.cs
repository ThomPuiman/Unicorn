using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Publishing;
using Sitecore.Publishing.Pipelines.Publish;

namespace Unicorn.Publishing
{
	/// <summary>
	/// Maintains a manual publish queue that arbitrary items can be added to
	/// See http://www.velir.com/blog/index.php/2013/11/22/how-to-create-a-custom-publish-queue-in-sitecore/ among other sources
	/// </summary>
	public class ManualPublishQueueHandler : PublishProcessor
	{
        private static readonly Dictionary<ID, string> ManuallyAddedCandidates = new Dictionary<ID, string>();
	    private static Database[] targetDbs;

		public static void AddItemToPublish(ID itemId, string dbName)
		{
		    if (!ManuallyAddedCandidates.ContainsKey(itemId))
		    {
		        ManuallyAddedCandidates.Add(itemId, dbName);
		    }
		}

		public static bool PublishQueuedItems(Database[] targets)
		{
		    targetDbs = targets;
			if (ManuallyAddedCandidates.Count == 0) return false;

			// the trigger item simply has to exist so the publish occurs - our queue will then be injected
		    foreach (ID targetID in ManuallyAddedCandidates.Keys)
		    {
                var item = Database.GetDatabase(ManuallyAddedCandidates[targetID]).GetItem(targetID);
                PublishManager.PublishItem(item, targets, new[] { item.Language }, true, false);
		    }

			return true;
		}

		public override void Process(PublishContext context)
		{
			var candidates = new List<PublishingCandidate>();

		    var currentQueue = ManuallyAddedCandidates.ToDictionary(x => x.Key, x => x.Value);

			ID candidate;
			do
			{
                if ((candidate = currentQueue.Keys.FirstOrDefault()) == (ID)null) break;

				candidates.Add(new PublishingCandidate(candidate, context.PublishOptions));
                currentQueue.Remove(candidate);
			} while (candidate != (ID)null);

		    targetDbs = targetDbs.Where(x => x.Name != context.PublishOptions.TargetDatabase.Name).ToArray();

		    if (targetDbs.Any())
		    {
		        ManuallyAddedCandidates.Clear();
		    }

			context.Queue.Add(candidates);
		}
	}
}
