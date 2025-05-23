using System;
using System.Collections.Generic;

namespace TimeTracker
{
    public class TrackRecord
    {
        public string Task { get; set; }
        public DateTime StartTime { get; set; }
        public TimeSpan Duration { get; set; }
        public string Comment { get; set; }
    }

    public class HistoryService
    {
        public List<TrackRecord> Records { get; private set; } = new List<TrackRecord>();

        public void AddRecord(string task, DateTime start, TimeSpan duration, string comment)
        {
            Records.Add(new TrackRecord
            {
                Task = task,
                StartTime = start,
                Duration = duration,
                Comment = comment
            });
        }
    }
}