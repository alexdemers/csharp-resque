C# Resque
============
C# Resque is full port of Chris Boulton's [php-resque](https://github.com/chrisboulton/php-resque). This library can queue and execute jobs using Redis. It is fully compatible with php-resque as in, if a job was enqueued with php-resque, C# Resque should be able to run it without hiccups.

## Installing
### Using NuGet
https://nuget.org/packages/CSharp-Resque/

### Importing Project
If you do not use NuGet to setup C# Resque, you will need the import its dependencies. Further information on how to get them are available on their respective sites.
* [Newtonsoft.Json](http://json.codeplex.com/)
* [ServiceStack.Redis](http://www.servicestack.net/)

## Getting Started
Once you have C# Resque in your project's References, you need to do a minor setup. First, because of .NET's strictly-typed nature, you need to tell Resque about every class you will be using in your queues. For example:
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

## Jobs
### Queueing Jobs
Queuing jobs is pretty much like php-resque or even the original ruby Resque, call the static method `Enqueue` on the Resque object.

```csharp
var arguments = new JObject { new JProperty("arg1", "value1"), new JProperty("arg2", false) };
Resque.Enqueue("object:action", "MyJob", arguments);
```

### Declaring Jobs
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
You can access the 3 property in your job:
* `JObject Args`: The arguments of your job
* `string Queue`: The name of the queue
* `Job Job`: The job that is being executed

Just like php-resque, c# Resque supports the `SetUp` and `TearDown` methods in your job to do job pre-processing and job post-processing.

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
