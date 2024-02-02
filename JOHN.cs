// There's 100% some way of optimising this, not bothered at the moment though

using MongoDB.Driver;
using MongoDB.Bson;

class Program
{
  private static MongoClient client;
  private static IMongoCollection<BsonDocument> collection;
  public static void Main()
  {
    ConnectToMongoDB();
    Menu();
  }

  private static void Menu() {
    Console.WriteLine("Please choose an operation:\n1. Create new task\n2. Delete existing task\n3. Sort and display tasks\n4. Exit");
    int choice = int.Parse(Console.ReadLine());
    switch (choice)
    {
      case 1:
        Create();
        Menu();
        break;
      case 2:
        Delete();
        Menu();
        break;
      case 3:
        Sort();
        Menu();
        break;
      case 4:
        Environment.Exit(0);
        break;
      default:
        Console.WriteLine("Invalid choice. Please choose a number between 1 and 4.");
        Menu();
        break;
    }
  }

  private static void ConnectToMongoDB()
  {
    string connectionUri;

    if(File.Exists(".env")) {
      Console.WriteLine("Connecting...");
      using(var sr = new StreamReader(".env")) {
        connectionUri = sr.ReadToEnd();
      }
    } else {  
      Console.WriteLine("Create a .env file with your connection details to avoid receiving this message.");
      Console.WriteLine("Enter connection URL for MongoDB server: ");
      connectionUri = Console.ReadLine();
    }

      var settings = MongoClientSettings.FromConnectionString(connectionUri);
      settings.ServerApi = new ServerApi(ServerApiVersion.V1);
      Console.WriteLine(settings);
      client = new MongoClient(settings);  

    try
    {
      client.GetDatabase("admin").RunCommand<BsonDocument>(new BsonDocument("ping", 1));
      Console.WriteLine("Pinged your deployment. JOHN is now online!");
      var dbList = client.ListDatabases().ToList();
      Console.WriteLine("Enter the name of your database, the list of databases on this server is: ");
      foreach (var db in dbList)
      {
        Console.WriteLine(db);
      }
      string dbName = Console.ReadLine();

      try
      {
        var database = client.GetDatabase(dbName);
        Console.WriteLine("Enter the name of the collection you want to use: ");
        var collectionNames = database.ListCollectionNames().ToList();
        foreach (var collectionName in collectionNames)
        {
            Console.WriteLine(collectionName);
        }
        string selectedCollection = Console.ReadLine();
        collection = database.GetCollection<BsonDocument>(selectedCollection);
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex);
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine(ex);
    }
  }

  private static void DeleteDue() {
    // Delete overdue tasks 
  }

  private static void Create()
  {
    List<BsonDocument> tasks = new List<BsonDocument>();

    while (true)
    {
      Console.WriteLine("Enter task details or type 'Submit' to finish:");
      Console.WriteLine("Subject: ");
      string subject = Console.ReadLine();

      if (subject.Trim().ToLower() == "submit")
      {
        break;
      }

      Console.WriteLine("Task Description: ");
      string taskDesc = Console.ReadLine();
      Console.WriteLine("Deadline: ");
      int deadline = int.Parse(Console.ReadLine());

      var document = new BsonDocument
            {
                {"Subject", subject},
                {"Description", taskDesc},
                {"Deadline", deadline}
            };

      tasks.Add(document);
    }

    InsertTasks(tasks);
  }

  private static void InsertTasks(List<BsonDocument> tasks)
  {
    try
    {
      collection.InsertMany(tasks);
      Console.WriteLine($"{tasks.Count} documents inserted successfully!");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error inserting documents: {ex}");
    }
  }

  private static void Delete()
  {
    while (true)
    {
      Console.WriteLine("Please choose a deletion option:\n1. Delete by date\n2. Delete by subject\n3. Delete by name\n4. Back to main menu");
      int deleteChoice = int.Parse(Console.ReadLine());

      switch (deleteChoice)
      {
        case 1:
          DeleteByDate();
          Menu();
          break;
        case 2:
          DeleteBySubject();
          Menu();
          break;
        case 3:
          DeleteByName();
          Menu();
          break;
        case 4:
          Menu();
          break;
        default:
          Console.WriteLine("Invalid choice. Please choose an option between 1 and 4.");
          Menu();
          break;
      }
    }
  }
    private static void DeleteByDate()
  {
    Console.WriteLine("Enter the date to delete tasks (format: DDMMYYYY): ");
    string dateInput = Console.ReadLine();
    if (DateTime.TryParseExact(dateInput, "ddMMyyyy", null, System.Globalization.DateTimeStyles.None, out DateTime dateToDelete))
    {
      var filter = Builders<BsonDocument>.Filter.Eq("Deadline", dateToDelete);
      DeleteTasks(filter);
    }
    else
    {
      Console.WriteLine("Invalid date format. Please use the format DDMMYYYY.");
    }
  }

  private static void DeleteBySubject()
  {
    Console.WriteLine("Enter the subject to delete tasks: ");
    string subjectToDelete = Console.ReadLine();
    var filter = Builders<BsonDocument>.Filter.Eq("Subject", subjectToDelete);
    DeleteTasks(filter);
  }

  private static void DeleteByName()
  {
    Console.WriteLine("Enter the name to delete tasks: ");
    string nameToDelete = Console.ReadLine();
    var filter = Builders<BsonDocument>.Filter.Eq("Name", nameToDelete);
    DeleteTasks(filter);
  }

  private static void DeleteTasks(FilterDefinition<BsonDocument> filter)
  {
    try
    {
      var result = collection.DeleteMany(filter);
      Console.WriteLine($"{result.DeletedCount} documents deleted successfully!");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error deleting documents: {ex}");
    }
  }

  private static void Sort() {
    Console.WriteLine("Choose sorting method\n1. Unsorted\n2. Most urgent to least\n3.By subject\n4.Back to main menu");
    int choice = int.Parse(Console.ReadLine());

    switch(choice) {
      case 1:
        Unsorted();
        Menu();
        break;
      case 2:
        Urgent();
        Menu();
        break;
      case 3:
        Console.WriteLine("Subject name to sort by: ");
        string subject = Console.ReadLine();
        Subject(subject);
        Menu();
        break;
      case 4:
        Menu();
        break;
    }
  }

    private static void Display(BsonArray tasks)
    {
        Console.WriteLine("--------------------------------------------------------");
        Console.WriteLine("{0,-15} {1,-15} {2,-10} {3,-10}", "Subject", "Task", "Priority", "Deadline");
        Console.WriteLine("--------------------------------------------------------");
        foreach (var task in tasks)
        {
            Console.ForegroundColor = GetTaskColor(task.ToBsonDocument());
            // var deadline = DateTime.ParseExact(task["Deadline"].ToString(), "ddMMyyyy", CultureInfo.InvariantCulture); .ToString("dd/MM/yyyy")
            Console.WriteLine($"{task["Subject"],-15} {task["Task"],-15} {GetPriority(task.ToBsonDocument()),-10} {task["Deadline"],-10}");
            Console.ResetColor();
        }

        Console.WriteLine("--------------------------------------------------------");
    }

    private static ConsoleColor GetTaskColor(BsonDocument task)
    {
        var deadline = task.GetValue("Deadline").ToUniversalTime(); 

        TimeSpan timeDifference = deadline - DateTime.UtcNow;

        if (timeDifference.TotalDays <= 7)
        {
            return ConsoleColor.Red; 
        }
        else if (timeDifference.TotalDays <= 14)
        {
            return ConsoleColor.Yellow; 
        }
        else
        {
            return ConsoleColor.Green;
        }
    } 

    private static string GetPriority(BsonDocument task)
    {
        var deadline = task.GetValue("Deadline").ToUniversalTime();

        TimeSpan timeDifference = deadline - DateTime.UtcNow;

        if (timeDifference.TotalDays <= 7)
        {
            return "High";
        }
        else if (timeDifference.TotalDays <= 14)
        {
            return "Medium";
        }
        else
        {
            return "Low";
        }
    }

  private static void Unsorted(){
    var filter = Builders<BsonDocument>.Filter.Empty;
    var tasks = collection.Find(filter).ToList();
    Display(new BsonArray(tasks));
  }

  private static void Urgent(){
    var filter = Builders<BsonDocument>.Filter.Empty;
    var sortDefinition = Builders<BsonDocument>.Sort.Ascending("Deadline");
    var tasks = collection.Find(filter).Sort(sortDefinition).ToList();
    Display(new BsonArray(tasks));
  }

  private static void Subject(string Subject) {
    var filter = Builders<BsonDocument>.Filter.Eq("Subject", Subject);
    var tasks = collection.Find(filter).ToList();
    Display(new BsonArray(tasks));
  }
}
