csharp-resque
============
csharp-resque is full port of Chris Boulton's [php-resque](https://github.com/chrisboulton/php-resque).


## Installing
### Using NuGet
https://nuget.org/packages/CSharp-Resque/

### Importing Project

#### Dependencies
* [Newtonsoft.Json](http://json.codeplex.com/)
* [ServiceStack.Redis](http://www.servicestack.net/)

## Getting Started
Once you have Resque (csharp-resque) in your project's References, you need to do a minor setup. First, because of .NET's strictly-typed nature, you need to tell Resque about every class you will be using in your queues. For example:
```csharp
Resque.Resque.AddJob("MyJob", typeof(MyJob));
```
After, you need to setup the Redis config:
```csharp
Resque.Resque.SetRedis("localhost", 6379, 0);
```
That's it. The minimum setup is done. If you want to attach events to your worker, this is the place to do them. If not, you are now ready to start the worker:
```csharp
new Resque.Worker("*").Work(5);
```

## Declaring Jobs
Declaring a job is straightforward. You create a new class that extends Resque.Jobs.BaseJob and you override the method `public void Perform`. At it's most basic form, a Job should have the minimum:

```csharp
class MyJob : Resque.Jobs.BaseJob
{
	public override void Perform()
	{
		JObject arguments = Args; // This is how you get your arguments from your job.
	}
}
```

## Attaching Events
One of the nice things from Chris' php implementation of Resque is that it supports events. If your application needs to do something when a job fails for example, simply attach an event using C#'s event handlers.
```csharp
Resque.Event.Failure += failureHandler;

private static void failureHandler(Exception exception, Job job, EventArgs eventArgs)
{
	Console.WriteLine(job.Queue + " failed.");
}
```

Supported events are:
* `Failure`: triggered when a job fails (throws an exception, for example).
* `BeforePerform`: triggered when the job is ready to be executed, just before executing the optional `SetUp` method.
* `AfterPerform`: triggered when the job is done executing, just after executing the optional `TearDown` method.
* `AfterEnqueue`: triggered when a job is manually enqueued using `Resque.Enqueue`.
