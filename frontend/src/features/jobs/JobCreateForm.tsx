import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { createJobSchema, type CreateJobSchema } from "./schema";
import { useCreateJob } from "./hooks";

export function JobCreateForm() {
  const createJob = useCreateJob();
  const form = useForm<CreateJobSchema>({
    resolver: zodResolver(createJobSchema),
    defaultValues: {
      repositoryUrl: "",
      branch: ""
    }
  });

  const onSubmit = form.handleSubmit(async (values) => {
    await createJob.mutateAsync(values);
    form.reset();
  });

  return (
    <form className="space-y-4" onSubmit={onSubmit}>
      <div className="space-y-1">
        <label className="block text-[11px] font-bold uppercase tracking-[0.12em] text-ink" htmlFor="repositoryUrl">
          GitHub Repo URL <span className="text-rose">*</span>
        </label>
        <input
          id="repositoryUrl"
          type="url"
          placeholder="https://github.com/owner/repo"
          className="h-10 w-full rounded border border-outline bg-surface px-3 text-sm text-ink outline-none transition focus:border-sky focus:ring-1 focus:ring-sky"
          {...form.register("repositoryUrl")}
        />
        {form.formState.errors.repositoryUrl ? (
          <p className="text-xs font-medium text-rose">{form.formState.errors.repositoryUrl.message}</p>
        ) : null}
      </div>

      <div className="space-y-1">
        <label className="block text-[11px] font-bold uppercase tracking-[0.12em] text-ink" htmlFor="branch">
          Branch
        </label>
        <input
          id="branch"
          type="text"
          placeholder="main"
          className="h-10 w-full rounded border border-outline bg-surface px-3 text-sm text-ink outline-none transition focus:border-sky focus:ring-1 focus:ring-sky"
          {...form.register("branch")}
        />
      </div>

      {createJob.isError ? (
        <p className="text-sm font-medium text-rose">
          {(createJob.error as Error).message || "Could not create job."}
        </p>
      ) : null}

      <button
        type="submit"
        disabled={createJob.isPending}
        className="flex h-10 w-full items-center justify-center gap-2 rounded border border-slate-900 bg-slate-900 px-4 text-[11px] font-bold uppercase tracking-[0.14em] text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
      >
        {createJob.isPending ? "Submitting..." : "Create job"}
      </button>
    </form>
  );
}
