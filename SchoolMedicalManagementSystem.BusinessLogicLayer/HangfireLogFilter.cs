using Hangfire.States;
using Hangfire.Storage;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer;

public class HangfireLogFilter : IElectStateFilter, IApplyStateFilter
{
    public void OnStateElection(ElectStateContext context)
    {
        var jobMethod = $"{context.BackgroundJob.Job.Type.Name}.{context.BackgroundJob.Job.Method.Name}";
        Console.WriteLine($"[Hangfire] Job {context.BackgroundJob.Id} ({jobMethod}) -> {context.CandidateState.Name}");
    }

    public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
        var jobMethod = $"{context.BackgroundJob.Job.Type.Name}.{context.BackgroundJob.Job.Method.Name}";
        var jobId = context.BackgroundJob.Id;
        
        switch (context.NewState)
        {
            case SucceededState:
                Console.WriteLine($"[Hangfire] {jobId} succeeded: {jobMethod}");
                break;
            case FailedState failedState:
                Console.WriteLine($"[Hangfire] {jobId} failed: {jobMethod} - {failedState.Exception?.Message}");
                break;
            case EnqueuedState:
                Console.WriteLine($"[Hangfire] {jobId} enqueued: {jobMethod}");
                break;
            case ProcessingState:
                Console.WriteLine($"[Hangfire] {jobId} processing: {jobMethod}");
                break;
            case ScheduledState scheduledState:
                Console.WriteLine($"[Hangfire] {jobId} scheduled: {scheduledState.EnqueueAt:HH:mm:ss}");
                break;
        }
    }

    public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
    }
}