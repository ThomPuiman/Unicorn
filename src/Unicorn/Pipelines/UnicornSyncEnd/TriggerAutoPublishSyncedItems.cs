using System.Collections.Generic;
using System.Linq;
using System.Web;
using Kamsar.WebConsole;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Diagnostics;
using Unicorn.Publishing;

namespace Unicorn.Pipelines.UnicornSyncEnd
{
	/// <summary>
	/// Triggers an auto-publish of the synced items in the processed configurations
	/// </summary>
	public class TriggerAutoPublishSyncedItems : IUnicornSyncEndProcessor
	{
		public string PublishTriggerItemId { get; set; }
		private readonly List<string> _targetDatabases = new List<string>();

		public void AddTargetDatabase(string database)
		{
			_targetDatabases.Add(database);
		}

		public void Process(UnicornSyncEndPipelineArgs args)
		{
			Assert.IsNotNullOrEmpty(PublishTriggerItemId, "Must set PublishTriggerItemId parameter.");

			if (_targetDatabases == null || _targetDatabases.Count == 0) return;

		    if (!string.IsNullOrWhiteSpace(HttpContext.Current.Request.QueryString["publishDbs"]))
		    {
		        List<string> dbNames = HttpContext.Current.Request.QueryString["publishDbs"].Split(',').ToList();
                dbNames.ForEach(AddTargetDatabase);
		    }

			var dbs = _targetDatabases.Select(Factory.GetDatabase).ToArray();

		    string[] deepPublish = PublishTriggerItemId.Split(',');

		    if (HttpContext.Current.Request.QueryString["fullPublish"] == "true")
		    {
		        foreach (string path in deepPublish)
		        {
		            var extractPath = path.Split(':');
                    var trigger = Factory.GetDatabase(extractPath[0]).GetItem(extractPath[1]);
                    ManualPublishQueueHandler.AddItemToPublish(trigger.ID, extractPath[0]);
		        }
		    }


			Assert.IsTrue(dbs.Length > 0, "No valid databases specified to publish to.");

			if (ManualPublishQueueHandler.PublishQueuedItems(dbs))
			{
				Log.Info("Unicorn: initiated publishing of synced items.", this);
			}
		}
	}
}
