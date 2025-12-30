using System;
using System.Collections.Generic;

namespace Physical.Therapy.UI
{
    /// <summary>
    /// Represents the body areas that an exercise targets.
    /// Order matches UI: Chest, Back, Arms, Abdominal, Legs, Shoulders
    /// </summary>
    [Flags]
    public enum BodyArea
    {
        Chest = 0,
        Back = 1 << 1,
        Arms = 1 << 2,
        Abdominal = 1 << 3,
        Legs = 1 << 4,
        Shoulders = 1 << 5,
        None = 1 << 6,
    }

    /// <summary>
    /// Represents the difficulty level of an exercise.
    /// Order matches UI: Easy, Medium, Hard
    /// </summary>
    public enum Difficulty
    {
        Easy = 0,
        Medium = 1,
        Hard = 2,
        None = 3,
    }

    /// <summary>
    /// Contains metadata for a pose/exercise including body area and difficulty
    /// </summary>
    [Serializable]
    public class PoseMetadata
    {
        /// <summary>
        /// The pose file name (without .json extension)
        /// </summary>
        public string PoseName { get; set; }

        /// <summary>
        /// The body areas this exercise targets
        /// </summary>
        public BodyArea BodyAreas { get; set; }

        /// <summary>
        /// The difficulty level of this exercise
        /// </summary>
        public Difficulty Difficulty { get; set; }

        /// <summary>
        /// Optional description of the exercise
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Icon filename for this pose (relative to the icons folder)
        /// </summary>
        public string IconFileName { get; set; }

        public PoseMetadata()
        {
            PoseName = "";
            BodyAreas = BodyArea.None;
            Difficulty = Difficulty.Easy;
            Description = "";
            IconFileName = "";
        }

        public PoseMetadata(string poseName, BodyArea bodyAreas, Difficulty difficulty, string description = "", string iconFileName = "")
        {
            PoseName = poseName;
            BodyAreas = bodyAreas;
            Difficulty = difficulty;
            Description = description;
            IconFileName = iconFileName;
        }

        /// <summary>
        /// Check if this pose targets a specific body area
        /// </summary>
        public bool TargetsBodyArea(BodyArea area)
        {
            return (BodyAreas & area) != 0;
        }

        /// <summary>
        /// Get a display-friendly list of body areas
        /// </summary>
        public List<string> GetBodyAreaNames()
        {
            var names = new List<string>();
            foreach (BodyArea area in Enum.GetValues(typeof(BodyArea)))
            {
                if (area != BodyArea.None && (BodyAreas & area) != 0)
                {
                    names.Add(area.ToString());
                }
            }
            return names;
        }

        /// <summary>
        /// Parse body area from string (comma-separated list)
        /// </summary>
        public static BodyArea ParseBodyAreas(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return BodyArea.None;

            BodyArea result = BodyArea.None;
            string[] parts = input.Split(new[] { ',', '|', '+' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string part in parts)
            {
                string trimmed = part.Trim().ToLower();
                switch (trimmed)
                {
                    case "chest":
                        result |= BodyArea.Chest;
                        break;
                    case "back":
                        result |= BodyArea.Back;
                        break;
                    case "arms":
                        result |= BodyArea.Arms;
                        break;
                    case "abdominal":
                    case "abs":
                    case "core":
                        result |= BodyArea.Abdominal;
                        break;
                    case "legs":
                        result |= BodyArea.Legs;
                        break;
                    case "shoulders":
                        result |= BodyArea.Shoulders;
                        break;
                }
            }
            return result;
        }

        /// <summary>
        /// Parse difficulty from string
        /// </summary>
        public static Difficulty ParseDifficulty(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Difficulty.Easy;

            string trimmed = input.Trim().ToLower();
            switch (trimmed)
            {
                case "1":
                case "easy":
                    return Difficulty.Easy;
                case "2":
                case "medium":
                    return Difficulty.Medium;
                case "3":
                case "hard":
                    return Difficulty.Hard;
                default:
                    return Difficulty.Easy;
            }
        }
    }
}
