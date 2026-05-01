import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { useNavigate } from "react-router-dom";
import { useCreateJob, useRepositoryBranches } from "./hooks";
import { createJobSchema, type CreateJobSchema } from "./schema";
import { StackBadge } from "../../components/StackBadge";

export function JobCreateForm() {
  const navigate = useNavigate();
  const createJob = useCreateJob();
  const branchLookup = useRepositoryBranches();
  const [branches, setBranches] = useState<string[]>([]);
  const [detectedStack, setDetectedStack] = useState<string | null>(null);
  const form = useForm<CreateJobSchema>({
    resolver: zodResolver(createJobSchema),
    defaultValues: {
      name: "",
      repositoryUrl: "",
      branch: "",
      projectPath: ""
    }
  });

  const onSubmit = form.handleSubmit(async (values) => {
    const createdJob = await createJob.mutateAsync({
      name: values.name.trim(),
      repositoryUrl: values.repositoryUrl.trim(),
      branch: values.branch?.trim() || undefined,
      projectPath: values.projectPath?.trim() || undefined
    });

    navigate(`/jobs/${createdJob.id}`);
  });

  async function handleLoadBranches() {
    const repositoryUrl = form.getValues("repositoryUrl").trim();
    const projectPath = form.getValues("projectPath")?.trim();
    const isValid = await form.trigger(["repositoryUrl", "projectPath"]);
    if (!isValid) {
      return;
    }

    try {
      const inspection = await branchLookup.mutateAsync({
        repositoryUrl,
        projectPath: projectPath || undefined
      });
      form.clearErrors("repositoryUrl");
      form.clearErrors("projectPath");
      setBranches(inspection.branches);
      setDetectedStack(inspection.detectedStack ?? null);
      form.setValue("projectPath", inspection.projectPath ?? "", { shouldValidate: true });

      const currentBranch = form.getValues("branch")?.trim();
      if (!currentBranch || !inspection.branches.includes(currentBranch)) {
        form.setValue("branch", inspection.branches[0] ?? "", { shouldValidate: true });
      }
    } catch (error) {
      form.setError(projectPath ? "projectPath" : "repositoryUrl", {
        type: "manual",
        message: (error as Error).message || "Could not inspect repository."
      });
      setBranches([]);
      setDetectedStack(null);
    }
  }

  function handleRepositoryBlur() {
    const currentName = form.getValues("name").trim();
    if (currentName) {
      return;
    }

    const suggestedName = suggestNameFromRepositoryUrl(form.getValues("repositoryUrl"));
    if (!suggestedName) {
      return;
    }

    form.setValue("name", suggestedName, {
      shouldDirty: true,
      shouldValidate: true
    });
  }

  return (
    <form className="space-y-4" onSubmit={onSubmit}>
      <div className="space-y-1">
        <label className="block text-[11px] font-bold uppercase tracking-[0.12em] text-ink" htmlFor="name">
          Job Name <span className="text-rose">*</span>
        </label>
        <input
          id="name"
          type="text"
          placeholder="marketing-site"
          className="h-10 w-full rounded border border-outline bg-surface px-3 text-sm text-ink outline-none transition focus:border-sky focus:ring-1 focus:ring-sky"
          {...form.register("name")}
        />
        {form.formState.errors.name ? (
          <p className="text-xs font-medium text-rose">{form.formState.errors.name.message}</p>
        ) : null}
      </div>

      <div className="space-y-1">
        <label className="block text-[11px] font-bold uppercase tracking-[0.12em] text-ink" htmlFor="repositoryUrl">
          GitHub Repo URL <span className="text-rose">*</span>
        </label>
        <div className="flex flex-col gap-2 md:flex-row">
          <input
            id="repositoryUrl"
            type="url"
            placeholder="https://github.com/owner/repo"
            className="h-10 w-full rounded border border-outline bg-surface px-3 text-sm text-ink outline-none transition focus:border-sky focus:ring-1 focus:ring-sky"
            {...form.register("repositoryUrl", {
              onChange: () => {
                setBranches([]);
                setDetectedStack(null);
              },
              onBlur: handleRepositoryBlur
            })}
          />
          <button
            type="button"
            onClick={() => void handleLoadBranches()}
            disabled={branchLookup.isPending}
            className="inline-flex h-10 items-center justify-center gap-2 rounded border border-outline bg-white px-4 text-[11px] font-bold uppercase tracking-[0.12em] text-ink transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60 md:min-w-40"
          >
            <span className="material-symbols-outlined text-[16px]">call_split</span>
            {branchLookup.isPending ? "Loading..." : "Load Branches"}
          </button>
        </div>
        {form.formState.errors.repositoryUrl ? (
          <p className="text-xs font-medium text-rose">{form.formState.errors.repositoryUrl.message}</p>
        ) : (
          <p className="text-xs text-steel">Branch lookup currently uses repository refs available via git.</p>
        )}
        {detectedStack ? (
          <div className="pt-1">
            <p className="mb-2 text-[11px] font-bold uppercase tracking-[0.12em] text-steel">Detected Stack</p>
            <StackBadge stack={detectedStack} />
          </div>
        ) : null}
      </div>

      <div className="space-y-1">
        <label className="block text-[11px] font-bold uppercase tracking-[0.12em] text-ink" htmlFor="projectPath">
          Project Path
        </label>
        <input
          id="projectPath"
          type="text"
          placeholder="apps/frontend"
          className="h-10 w-full rounded border border-outline bg-surface px-3 text-sm text-ink outline-none transition focus:border-sky focus:ring-1 focus:ring-sky"
          {...form.register("projectPath", {
            onChange: () => {
              setDetectedStack(null);
            }
          })}
        />
        {form.formState.errors.projectPath ? (
          <p className="text-xs font-medium text-rose">{form.formState.errors.projectPath.message}</p>
        ) : (
          <p className="text-xs text-steel">
            Optional relative path inside the repository, for example `frontend`, `backend/api`, or `apps/web`.
          </p>
        )}
      </div>

      <div className="space-y-1">
        <label className="block text-[11px] font-bold uppercase tracking-[0.12em] text-ink" htmlFor="branch">
          Branch
        </label>
        <select
          id="branch"
          className="h-10 w-full rounded border border-outline bg-surface px-3 text-sm text-ink outline-none transition focus:border-sky focus:ring-1 focus:ring-sky"
          {...form.register("branch")}
        >
          <option value="">Use repository default branch</option>
          {branches.map((branch) => (
            <option key={branch} value={branch}>
              {branch}
            </option>
          ))}
        </select>
        <p className="text-xs text-steel">
          {branches.length > 0
            ? `Loaded ${branches.length} branches from the repository.`
            : "Load branches to pick a specific ref, or leave this empty to use the default branch."}
        </p>
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

function suggestNameFromRepositoryUrl(repositoryUrl: string) {
  try {
    const url = new URL(repositoryUrl);
    const segments = url.pathname.split("/").filter(Boolean);
    const repositoryName = segments[segments.length - 1];
    return repositoryName?.replace(/\.git$/i, "") ?? "";
  } catch {
    return "";
  }
}
