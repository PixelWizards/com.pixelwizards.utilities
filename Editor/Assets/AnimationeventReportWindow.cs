using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PixelWizards.UnityEditorTools.Animation
{
	public sealed class AnimationEventReportWindow : EditorWindow
	{
		private DefaultAsset _folder;
		private Vector2 _scrollPosition;
		private string _report = string.Empty;

		[MenuItem("Assets/Animation/Animation Event Report")]
		private static void Open()
		{
			GetWindow<AnimationEventReportWindow>("Animation Events");
		}

		private void OnGUI()
		{
			EditorGUILayout.LabelField("Animation Event Report", EditorStyles.boldLabel);

			EditorGUILayout.HelpBox(
				"Select a Project folder to scan all AnimationClips beneath it, including clips embedded in FBX assets.",
				MessageType.Info);

			_folder = (DefaultAsset)EditorGUILayout.ObjectField(
				"Folder",
				_folder,
				typeof(DefaultAsset),
				false);

			using (new EditorGUI.DisabledScope(!IsValidFolder(_folder)))
			{
				if (GUILayout.Button("Scan Folder"))
				ScanFolder();

				if (GUILayout.Button("Export CSV"))
					ExportCsv();
			}

			EditorGUILayout.Space();

			using (var scrollView = new EditorGUILayout.ScrollViewScope(_scrollPosition))
			{
				_scrollPosition = scrollView.scrollPosition;

				EditorGUILayout.TextArea(
					_report,
					GUILayout.ExpandHeight(true));
			}
		}

		private void ScanFolder()
		{
			string folderPath = AssetDatabase.GetAssetPath(_folder);

			if (!AssetDatabase.IsValidFolder(folderPath))
				throw new InvalidOperationException($"'{folderPath}' is not a valid Project folder.");

			List<AnimationEventRecord> records = CollectRecords(folderPath);

			_report = BuildReadableReport(folderPath, records);

			Debug.Log(_report);
		}

		private void ExportCsv()
		{
			string folderPath = AssetDatabase.GetAssetPath(_folder);

			if (!AssetDatabase.IsValidFolder(folderPath))
				throw new InvalidOperationException($"'{folderPath}' is not a valid Project folder.");

			List<AnimationEventRecord> records = CollectRecords(folderPath);

			string savePath = EditorUtility.SaveFilePanel(
				"Export Animation Event Report",
				Application.dataPath,
				"AnimationEventReport.csv",
				"csv");

			if (string.IsNullOrWhiteSpace(savePath))
				return;

			File.WriteAllText(savePath, BuildCsv(records), Encoding.UTF8);

			EditorUtility.RevealInFinder(savePath);
		}

		private static List<AnimationEventRecord> CollectRecords(string folderPath)
		{
			string[] guids = AssetDatabase.FindAssets(
				"t:AnimationClip",
				new[] { folderPath });

			var records = new List<AnimationEventRecord>();

			foreach (string guid in guids)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(guid);

				UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);

				foreach (AnimationClip clip in assets.OfType<AnimationClip>())
				{
					// Unity may expose internal preview clips from imported assets.
					if (clip == null || clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
						continue;

					AnimationEvent[] animationEvents = AnimationUtility.GetAnimationEvents(clip);

					if (animationEvents.Length == 0)
					{
						records.Add(AnimationEventRecord.CreateEmpty(assetPath, clip));
						continue;
					}

					foreach (AnimationEvent animationEvent in animationEvents)
					{
						records.Add(new AnimationEventRecord(
							assetPath,
							clip.name,
							clip.length,
							clip.frameRate,
							animationEvent.time,
							GetNormalizedTime(clip, animationEvent.time),
							animationEvent.functionName,
							animationEvent.stringParameter,
							animationEvent.floatParameter,
							animationEvent.intParameter,
							animationEvent.objectReferenceParameter != null
								? animationEvent.objectReferenceParameter.name
								: string.Empty));
					}
				}
			}

			return records
				.OrderBy(record => record.AssetPath, StringComparer.OrdinalIgnoreCase)
				.ThenBy(record => record.ClipName, StringComparer.OrdinalIgnoreCase)
				.ThenBy(record => record.Time)
				.ToList();
		}

		private static string BuildReadableReport(
			string folderPath,
			IReadOnlyList<AnimationEventRecord> records)
		{
			var builder = new StringBuilder();

			int clipCount = records
				.Select(record => $"{record.AssetPath}|{record.ClipName}")
				.Distinct()
				.Count();

			int eventCount = records.Count(record => record.HasEvent);

			builder.AppendLine($"Folder: {folderPath}");
			builder.AppendLine($"Clips: {clipCount}");
			builder.AppendLine($"Events: {eventCount}");
			builder.AppendLine();

			foreach (IGrouping<string, AnimationEventRecord> assetGroup in
			         records.GroupBy(record => record.AssetPath))
			{
				builder.AppendLine(assetGroup.Key);

				foreach (IGrouping<string, AnimationEventRecord> clipGroup in
				         assetGroup.GroupBy(record => record.ClipName))
				{
					AnimationEventRecord first = clipGroup.First();

					builder.AppendLine(
						$"  Clip: {first.ClipName} " +
						$"(Length: {first.ClipLength:0.###}s, FPS: {first.FrameRate:0.##})");

					List<AnimationEventRecord> clipEvents = clipGroup
						.Where(record => record.HasEvent)
						.ToList();

					if (clipEvents.Count == 0)
					{
						builder.AppendLine("    No animation events.");
						continue;
					}

					foreach (AnimationEventRecord record in clipEvents)
					{
						builder.AppendLine(
							$"    {record.Time:0.###}s " +
							$"({record.NormalizedTime:P1}) — " +
							$"{record.FunctionName}" +
							BuildParameterSummary(record));
					}
				}

				builder.AppendLine();
			}

			return builder.ToString();
		}

		private static string BuildCsv(IReadOnlyList<AnimationEventRecord> records)
		{
			var builder = new StringBuilder();

			builder.AppendLine(
				"Asset Path,Clip Name,Clip Length,Frame Rate,Event Time,Normalized Time," +
				"Function,String Parameter,Float Parameter,Int Parameter,Object Parameter");

			foreach (AnimationEventRecord record in records)
			{
				builder.AppendLine(string.Join(",",
					EscapeCsv(record.AssetPath),
					EscapeCsv(record.ClipName),
					record.ClipLength.ToString("0.######"),
					record.FrameRate.ToString("0.######"),
					record.HasEvent ? record.Time.ToString("0.######") : string.Empty,
					record.HasEvent ? record.NormalizedTime.ToString("0.######") : string.Empty,
					EscapeCsv(record.FunctionName),
					EscapeCsv(record.StringParameter),
					record.HasEvent ? record.FloatParameter.ToString("0.######") : string.Empty,
					record.HasEvent ? record.IntParameter.ToString() : string.Empty,
					EscapeCsv(record.ObjectParameter)));
			}

			return builder.ToString();
		}

		private static string BuildParameterSummary(AnimationEventRecord record)
		{
			var parameters = new List<string>();

			if (!string.IsNullOrWhiteSpace(record.StringParameter))
				parameters.Add($"String=\"{record.StringParameter}\"");

			if (!Mathf.Approximately(record.FloatParameter, 0f))
				parameters.Add($"Float={record.FloatParameter:0.###}");

			if (record.IntParameter != 0)
				parameters.Add($"Int={record.IntParameter}");

			if (!string.IsNullOrWhiteSpace(record.ObjectParameter))
				parameters.Add($"Object={record.ObjectParameter}");

			return parameters.Count > 0
				? $" [{string.Join(", ", parameters)}]"
				: string.Empty;
		}

		private static float GetNormalizedTime(AnimationClip clip, float eventTime)
		{
			if (clip.length <= Mathf.Epsilon)
				return 0f;

			return Mathf.Clamp01(eventTime / clip.length);
		}

		private static bool IsValidFolder(DefaultAsset folder)
		{
			if (folder == null)
				return false;

			string path = AssetDatabase.GetAssetPath(folder);
			return AssetDatabase.IsValidFolder(path);
		}

		private static string EscapeCsv(string value)
		{
			if (string.IsNullOrEmpty(value))
				return string.Empty;

			string escaped = value.Replace("\"", "\"\"");
			return $"\"{escaped}\"";
		}

		private readonly struct AnimationEventRecord
		{
			public string AssetPath { get; }
			public string ClipName { get; }
			public float ClipLength { get; }
			public float FrameRate { get; }
			public float Time { get; }
			public float NormalizedTime { get; }
			public string FunctionName { get; }
			public string StringParameter { get; }
			public float FloatParameter { get; }
			public int IntParameter { get; }
			public string ObjectParameter { get; }

			public bool HasEvent => !string.IsNullOrWhiteSpace(FunctionName);

			public AnimationEventRecord(
				string assetPath,
				string clipName,
				float clipLength,
				float frameRate,
				float time,
				float normalizedTime,
				string functionName,
				string stringParameter,
				float floatParameter,
				int intParameter,
				string objectParameter)
			{
				AssetPath = assetPath;
				ClipName = clipName;
				ClipLength = clipLength;
				FrameRate = frameRate;
				Time = time;
				NormalizedTime = normalizedTime;
				FunctionName = functionName;
				StringParameter = stringParameter;
				FloatParameter = floatParameter;
				IntParameter = intParameter;
				ObjectParameter = objectParameter;
			}

			public static AnimationEventRecord CreateEmpty(
				string assetPath,
				AnimationClip clip)
			{
				return new AnimationEventRecord(
					assetPath,
					clip.name,
					clip.length,
					clip.frameRate,
					0f,
					0f,
					string.Empty,
					string.Empty,
					0f,
					0,
					string.Empty);
			}
		}
	}
}