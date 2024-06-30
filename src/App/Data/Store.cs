using System.Collections.Concurrent;

namespace App.Data;



public record Feedback(string? User, string Content);

public class DbApp
{
 public ConcurrentBag<Feedback> Feedbacks { get; } = [];
}

public class Store
{
    public static ConcurrentDictionary<string, DbApp> Apps { get; } = new();

    static Store (){
        Apps.GetOrAdd("test", new DbApp()); 
}
}
