import { zodResolver } from "@hookform/resolvers/zod";
import type { ReactNode } from "react";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { useNavigate } from "react-router-dom";
import { StackBadge } from "../../components/StackBadge";
import { createProjectSchema, type CreateProjectSchema } from "../jobs/schema";
import { useCreateProject, useRepositoryBranches } from "../jobs/hooks";

export function ProjectCreateForm() {
  const navigate = useNavigate();
  const createProject = useCreateProject();
  const branchLookup = useRepositoryBranches();
  const [branches, setBranches] = useState<string[]>([]);
  const [detectedStack, setDetectedStack] = useState<string | null>(null);
  const form = useForm<CreateProjectSchema>({
    resolver: zodResolver(createProjectSchema),
    defaultValues: {
      name: "",
      repositoryUrl: "",
      defaultBranch: "",
      defaultProjectPath: ""
    }
  });

  const onSubmit = form.handleSubmit(async (values) => {
    const project = await createProject.mutateAsync({
      name: values.name.trim(),
      repositoryUrl: values.repositoryUrl.trim(),
      defaultBranch: values.defaultBranch?.trim() || undefined,
      defaultProjectPath: values.defaultProjectPath?.trim() || undefined
    });

    navigate(`/projects/${project.id}`);
  });

  async function handleLoadBranches() {
    const repositoryUrl = form.getValues("repositoryUrl").trim();
    const projectPath = form.getValues("defaultProjectPath")?.trim();
    const isValid = await form.trigger(["repositoryUrl", "defaultProjectPath"]);
    if (!isValid) {
      return;
    }

    try {
      const inspection = await branchLookup.mutateAsync({
        repositoryUrl,
        projectPath: projectPath || undefined
      });
      form.clearErrors("repositoryUrl");
      form.clearErrors("defaultProjectPath");
      setBranches(inspection.branches);
      setDetectedStack(inspection.detectedStack ?? null);
      form.setValue("defaultProjectPath", inspection.projectPath ?? "", { shouldValidate: true });

      const currentBranch = form.getValues("defaultBranch")?.trim();
      if (!currentBranch || !inspection.branches.includes(currentBranch)) {
        form.setValue("defaultBranch", inspection.branches[0] ?? "", { shouldValidate: true });
      }
    } catch (error) {
      form.setError(projectPath ? "defaultProjectPath" : "repositoryUrl", {
        type: "manual",
        message: (error as Error).message || "Could not inspect repository."
      });
      setBranches([]);
      setDetectedStack(null);
    }
  }

  function handleRepositoryBlur() {
    if (form.getValues("name").trim()) {
      return;
    }

    const suggestedName = suggestNameFromRepositoryUrl(form.getValues("repositoryUrl"));
    if (suggestedName) {
      form.setValue("name", suggestedName, { shouldDirty: true, shouldValidate: true });
    }
  }

  return (
    <form className="space-y-4" onSubmit={onSubmit}>
      <Field label="Project Name" required error={form.formState.errors.name?.message}>
        <input
          type="text"
          placeholder="marketing-site"
          className="h-10 w-full rounded border border-outline bg-surface px-3 text-sm text-ink outline-none transition focus:border-sky focus:ring-1 focus:ring-sky"
          {...form.register("name")}
        />
      </Field>

      <Field label="GitHub Repo URL" required error={form.formState.errors.repositoryUrl?.message}>
        <div className="flex flex-col gap-2 md:flex-row">
          <input
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
      </Field>

      {detectedStack ? (
        <div className="pt-1">
          <p className="mb-2 text-[11px] font-bold uppercase tracking-[0.12em] text-steel">Detected Stack</p>
          <StackBadge stack={detectedStack} />
        </div>
      ) : null}

      <Field label="Default Project Path" error={form.formState.errors.defaultProjectPath?.message}>
        <input
          type="text"
          placeholder="apps/frontend"
          className="h-10 w-full rounded border border-outline bg-surface px-3 text-sm text-ink outline-none transition focus:border-sky focus:ring-1 focus:ring-sky"
          {...form.register("defaultProjectPath", {
            onChange: () => setDetectedStack(null)
          })}
        />
      </Field>

      <Field label="Default Branch">
        <select
          className="h-10 w-full rounded border border-outline bg-surface px-3 text-sm text-ink outline-none transition focus:border-sky focus:ring-1 focus:ring-sky"
          {...form.register("defaultBranch")}
        >
          <option value="">Use repository default branch</option>
          {branches.map((branch) => (
            <option key={branch} value={branch}>
              {branch}
            </option>
          ))}
        </select>
      </Field>

      {createProject.isError ? (
        <p className="text-sm font-medium text-rose">
          {(createProject.error as Error).message || "Could not create project."}
        </p>
      ) : null}

      <button
        type="submit"
        disabled={createProject.isPending}
        className="flex h-10 w-full items-center justify-center gap-2 rounded border border-slate-900 bg-slate-900 px-4 text-[11px] font-bold uppercase tracking-[0.14em] text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
      >
        {createProject.isPending ? "Submitting..." : "Create project"}
      </button>
    </form>
  );
}

function Field({ label, required, error, children }: { label: string; required?: boolean; error?: string; children: ReactNode }) {
  return (
    <div className="space-y-1">
      <label className="block text-[11px] font-bold uppercase tracking-[0.12em] text-ink">
        {label} {required ? <span className="text-rose">*</span> : null}
      </label>
      {children}
      {error ? <p className="text-xs font-medium text-rose">{error}</p> : null}
    </div>
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
