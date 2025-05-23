using System.Collections.Generic;

namespace TimeTracker
{
    public class TaskManager
    {
        public List<string> Tasks { get; private set; } = new List<string>();

        public void AddTask(string task)
        {
            if (!Tasks.Contains(task))
                Tasks.Add(task);
        }

        public void RemoveTask(string task)
        {
            Tasks.Remove(task);
        }

        public void EditTask(string oldTask, string newTask)
        {
            int idx = Tasks.IndexOf(oldTask);
            if (idx >= 0 && !Tasks.Contains(newTask))
                Tasks[idx] = newTask;
        }
    }
}