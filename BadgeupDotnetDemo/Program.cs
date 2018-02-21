using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BadgeUp;
using BadgeUp.Responses;
using BadgeUp.Types;


namespace BadgeupDotnetDemo
{
	class Program
	{
		private static BadgeUpClient _badgeUpClient;
		private static string _subjectId;
		static void Main(string[] args)
		{
			MainAsync().Wait();
		}

		static async Task MainAsync()
		{
			_badgeUpClient = new BadgeUpClient(System.Environment.GetEnvironmentVariable("INTEGRATION_API_KEY"));
			Random r = new Random();
			_subjectId = "sub-" + r.Next(0, 99999);
			LogMessage($"Subject {_subjectId} created");

			LogMessage("Sending event - 200 steps walked, ");
			var eventResponse = await _badgeUpClient.Event.SendV2Preview(new Event(_subjectId, "step", new Modifier() { Inc = 200 }));
			var progressResults = await _badgeUpClient.Progress.GetProgress(_subjectId);
			await LogProgress(progressResults);
			await LogEvent(eventResponse);
			LogMessage("\n\n");

			LogMessage("Sending event - 200 steps walked");
			eventResponse = await _badgeUpClient.Event.SendV2Preview(new Event(_subjectId, "step", new Modifier() { Inc = 200 }), true);
			await LogEvent(eventResponse);
			LogMessage("\n\n");

			LogMessage("Sending event - 20 000 steps walked");
			eventResponse = await _badgeUpClient.Event.SendV2Preview(new Event(_subjectId, "step", new Modifier() { Inc = 20000 }), true);
			await LogEvent(eventResponse);
		}

		private static async Task LogProgress(List<Progress> progressResults)
		{
			foreach (var progressResult in progressResults)
			{
				foreach (var criterion in progressResult.ProgressTree.Criteria.Where(x => x.Value > 0))
				{
					var criterionResult = await _badgeUpClient.Criterion.GetById(criterion.Key);
					LogMessage(
						$"Criterion  \"{criterionResult.Name}\" is {criterion.Value * 100:.##}% complete = {criterionResult.Evaluation.Threshold * criterion.Value} steps taken.");
				}
			}
		}

		private static async Task LogEvent(EventResponseV2Preview e)
		{
			LogMessage("Progress:");
			await LogProgress(e.Results[0].Progress.ToList());
			
			var steps = await _badgeUpClient.Metric.GetIndividualBySubject(_subjectId, "step");
			LogMessage($"Overall steps: {steps.Value}");
			if (e.Results.Count == 1)
				LogMessage("No achievements earned");

			for (int i = 1; i < e.Results.Count; i++)
			{
				string achievementId = e.Results[i].Event.Key
					.Substring(e.Results[i].Event.Key.IndexOf("earned:") + "earned:".Length,
						e.Results[i].Event.Key.Length - (e.Results[i].Event.Key.IndexOf("earned:") + "earned:".Length));
				var achievement = await _badgeUpClient.Achievement.GetById(achievementId);
				LogMessage($"Achievement \"{achievement.Name}\" earned.");
			}
		}

		public static void LogMessage(string message)
		{
			Console.WriteLine(message);
			//small pause to have time to read text in console
			Thread.Sleep(400);
		}
	}
}
