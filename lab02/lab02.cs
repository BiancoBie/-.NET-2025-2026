using System;
using System.Collections.Generic;
using System.Linq;

Console.WriteLine("~ Tema 2 la .NET - Simple Project and Task Manager ~");

//records si clonare cu 'with'
var task1 = new TodoTask("Sa ma duc la facultate", false, DateTime.Now.AddDays(3));
var task2 = new TodoTask("Sa fac tema la ML", true, DateTime.Now.AddDays(-1));

var project = new Project("Proiectul la .NET", new List<TodoTask> { task1 });
Console.WriteLine($"Proiect original: {project.Name} cu {project.Tasks.Count} task-uri");

var newProject = project with { Tasks = new List<TodoTask>(project.Tasks) { task2 } };
Console.WriteLine($"Proiect clonat: {newProject.Name} cu {newProject.Tasks.Count} task-uri");

//prop. init-only
var manager = new Manager { Name = "Florin Olariu", Team = ".NET Development", Email = "florin@company.com" };
Console.WriteLine($"Manager: {manager.Name} din echipa {manager.Team}");

//introducere task-uri din terminal
Console.WriteLine("\n~ Introducere task-uri ~");
var userTasks = new List<TodoTask>();

while (true)
{
    Console.Write("Introdu titlul unui task (sau 'gata' daca te-ai saturat sa introduci): ");
    var taskTitle = Console.ReadLine();
    
    if (string.IsNullOrEmpty(taskTitle) || taskTitle.ToLower() == "gata")
        break;
    
    Console.Write("L-ai terminat sau ai frecat menta? (y/n): ");
    var completedInput = Console.ReadLine();
    bool isCompleted = completedInput?.ToLower() == "y" || completedInput?.ToLower() == "yes";
    
    Console.Write("Data limita (dd/mm/yyyy) sau default pentru maine (ca asa le lasi tu pe ultima suta de metri): ");
    var dueDateInput = Console.ReadLine();
    DateTime dueDate = DateTime.Now.AddDays(1); // default maine
    
    if (!string.IsNullOrEmpty(dueDateInput) && 
        DateTime.TryParseExact(dueDateInput, "dd/MM/yyyy", null, 
                              System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
    {
        dueDate = parsedDate;
    }
    
    userTasks.Add(new TodoTask(taskTitle, isCompleted, dueDate));
    Console.WriteLine($" Task-ul cu titlul '{taskTitle}' a fost adaugat.\n");
}

Console.WriteLine("\n~ Lista de task-uri ~");
if (userTasks.Count == 0)
{
    Console.WriteLine("Nu s-a introdus niciun task.");
}
else
{
    for (int i = 0; i < userTasks.Count; i++)
    {
        var task = userTasks[i];
        var status = task.IsCompleted ? "Completat" : "Necompletat";
        var overdueText = task.DueDate < DateTime.Now && !task.IsCompleted ? " (overdue)" : "";
        Console.WriteLine($"{i + 1}. {task.Title} - {status} - Due: {task.DueDate:dd/MM/yyyy}{overdueText}");
    }
}

//pattern matching
AnalyzeObject(task1);
AnalyzeObject(project);
AnalyzeObject(manager);
AnalyzeObject("text random");

//static lambda filtering
var overdueTasks = userTasks.Where(IsOverdueAndNotCompleted).ToList();
Console.WriteLine($"Task-uri intarziate (leneso): {overdueTasks.Count}");

static void AnalyzeObject(object obj)
{
    var result = obj switch
    {
        TodoTask task => $"Task: {task.Title} - {(task.IsCompleted ? "Completat" : "Necompletat")}",
        Project proj => $"Project: {proj.Name} cu {proj.Tasks.Count} task-uri",
        _ => "Unknown type"
    };
    Console.WriteLine(result);
}

static bool IsOverdueAndNotCompleted(TodoTask task) => 
    !task.IsCompleted && task.DueDate < DateTime.Now;

public record TodoTask(string Title, bool IsCompleted, DateTime DueDate);
public record Project(string Name, List<TodoTask> Tasks);

public class Manager
{
    public string Name { get; init; } = "";
    public string Team { get; init; } = "";
    public string Email { get; init; } = "";
}
