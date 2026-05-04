import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import type { JobDetails } from "./types";
import { useCancelJob, useDeleteJob, useRebuildJob, useRestartContainer, useStartContainer, useStopContainer } from "./hooks";

export function JobActions({ job }: { job: JobDetails }) {
  const navigate = useNavigate();
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false);
  const [deleteCompleted, setDeleteCompleted] = useState(false);
  const redirectTimeoutRef = useRef<number | null>(null);
  const rebuildJob = useRebuildJob(job.id);
  const cancelJob = useCancelJob(job.id);
  const deleteJob = useDeleteJob(job.id);
  const startContainer = useStartContainer(job.id);
  const restartContainer = useRestartContainer(job.id);
  const stopContainer = useStopContainer(job.id);
  const hasRunnableImage = Boolean(job.currentImage?.imageTag || job.generatedImageTag);
  const isContainerRunning = job.containerStatus === "running";
  const canMutateContainer = hasRunnableImage && job.status !== "Running";
  const canOpenDeployment = Boolean(job.deploymentUrl && isContainerRunning);

  useEffect(() => {
    return () => {
      if (redirectTimeoutRef.current !== null) {
        window.clearTimeout(redirectTimeoutRef.current);
      }
    };
  }, []);

  function handleDelete() {
    setDeleteCompleted(false);
    setIsDeleteDialogOpen(true);
  }

  function handleCloseDeleteDialog() {
    if (deleteJob.isPending || deleteCompleted) {
      return;
    }

    setIsDeleteDialogOpen(false);
  }

  function handleConfirmDelete() {
    deleteJob.mutate(undefined, {
      onSuccess: () => {
        setDeleteCompleted(true);
        redirectTimeoutRef.current = window.setTimeout(() => navigate("/jobs"), 900);
      }
    });
  }

  return (
    <>
      <div className="grid gap-2 sm:min-w-[20rem]">
        <div className="grid grid-cols-3 gap-2">
          <button
            type="button"
            onClick={() => startContainer.mutate()}
            disabled={startContainer.isPending || !canMutateContainer || isContainerRunning}
            className="flex items-center justify-center gap-1 rounded border border-outline bg-white px-3 py-1.5 text-sm font-medium text-ink transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
          >
            <span className="material-symbols-outlined text-[16px]">play_arrow</span>
            Start
          </button>
          <button
            type="button"
            onClick={() => stopContainer.mutate()}
            disabled={stopContainer.isPending || !canMutateContainer || !["running", "restarting"].includes(job.containerStatus ?? "")}
            className="flex items-center justify-center gap-1 rounded border border-outline bg-white px-3 py-1.5 text-sm font-medium text-ink transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
          >
            <span className="material-symbols-outlined text-[16px]">stop</span>
            Stop
          </button>
          <button
            type="button"
            onClick={() => restartContainer.mutate()}
            disabled={restartContainer.isPending || !canMutateContainer}
            className="flex items-center justify-center gap-1 rounded border border-outline bg-white px-3 py-1.5 text-sm font-medium text-ink transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
          >
            <span className="material-symbols-outlined text-[16px]">restart_alt</span>
            Restart
          </button>
        </div>
        <div className="grid grid-cols-3 gap-2">
          {canOpenDeployment ? (
            <a
              href={job.deploymentUrl ?? undefined}
              target="_blank"
              rel="noreferrer"
              className="flex items-center justify-center gap-1 rounded border border-secondary bg-white px-3 py-1.5 text-sm font-medium text-secondary transition hover:bg-[rgba(211,228,254,0.35)]"
            >
              <span className="material-symbols-outlined text-[16px]">open_in_new</span>
              Open
            </a>
          ) : (
            <span className="flex items-center justify-center gap-1 rounded border border-outline bg-surface px-3 py-1.5 text-sm font-medium text-steel opacity-70">
              <span className="material-symbols-outlined text-[16px]">open_in_new</span>
              Open
            </span>
          )}
          <button
            type="button"
            onClick={() => rebuildJob.mutate()}
            disabled={rebuildJob.isPending || job.status === "Running"}
            className="flex items-center justify-center gap-1 rounded border border-slate-900 bg-slate-900 px-3 py-1.5 text-sm font-medium text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
          >
            <span className="material-symbols-outlined text-[16px]">refresh</span>
            Rebuild
          </button>
          <button
            type="button"
            onClick={() => cancelJob.mutate()}
            disabled={cancelJob.isPending || ["Succeeded", "Failed", "Canceled"].includes(job.status)}
            className="flex items-center justify-center gap-1 rounded border border-outline bg-surface px-3 py-1.5 text-sm font-medium text-ink transition hover:bg-variant disabled:cursor-not-allowed disabled:opacity-60"
          >
            <span className="material-symbols-outlined text-[16px]">cancel</span>
            Cancel
          </button>
        </div>
        <button
          type="button"
          onClick={handleDelete}
          disabled={deleteJob.isPending}
          className="flex items-center justify-center gap-1 rounded border border-rose-300 bg-rose-50 px-3 py-1.5 text-sm font-medium text-rose-700 transition hover:bg-rose-100 disabled:cursor-not-allowed disabled:opacity-60"
        >
          <span className="material-symbols-outlined text-[16px]">delete</span>
          Delete job
        </button>
      </div>

      {isDeleteDialogOpen ? (
        <DeleteJobDialog
          job={job}
          isDeleting={deleteJob.isPending}
          isDeleted={deleteCompleted}
          errorMessage={deleteJob.error instanceof Error ? deleteJob.error.message : null}
          onCancel={handleCloseDeleteDialog}
          onConfirm={handleConfirmDelete}
        />
      ) : null}
    </>
  );
}

function DeleteJobDialog({
  job,
  isDeleting,
  isDeleted,
  errorMessage,
  onCancel,
  onConfirm
}: {
  job: JobDetails;
  isDeleting: boolean;
  isDeleted: boolean;
  errorMessage: string | null;
  onCancel: () => void;
  onConfirm: () => void;
}) {
  const statusIcon = isDeleted ? "check_circle" : isDeleting ? "progress_activity" : "warning";
  const statusText = isDeleted
    ? "Wszystko zostało usunięte. Przenoszę do listy jobów."
    : isDeleting
      ? "Usuwam job, kontener, obrazy, artefakty i pliki robocze."
      : "Ta operacja usunie job wraz ze wszystkimi przypisanymi elementami.";

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/35 px-4 py-6" role="dialog" aria-modal="true" aria-labelledby="delete-job-title">
      <div className="w-full max-w-lg overflow-hidden rounded border border-outline bg-white shadow-xl">
        <div className="border-b border-outline bg-rose-50 px-5 py-4">
          <div className="flex items-start gap-3">
            <span className="material-symbols-outlined mt-0.5 text-[22px] text-rose-700">delete</span>
            <div className="min-w-0">
              <h2 id="delete-job-title" className="text-base font-semibold text-ink">
                Usunąć job?
              </h2>
              <p className="mt-1 break-words text-sm text-steel">{job.name}</p>
            </div>
          </div>
        </div>

        <div className="space-y-4 px-5 py-4">
          <div className="rounded border border-outline bg-surface px-3 py-3">
            <p className="text-[10px] font-bold uppercase tracking-[0.12em] text-steel">Zakres usuwania</p>
            <div className="mt-3 grid gap-2 text-sm text-ink">
              <DeleteScopeItem icon="deployed_code" label="Kontener i konfiguracja runtime" />
              <DeleteScopeItem icon="inventory_2" label="Historia obrazów i obrazy Dockera" />
              <DeleteScopeItem icon="description" label="Logi, artefakty i wygenerowane pliki" />
              <DeleteScopeItem icon="folder_delete" label="Workspace joba" />
            </div>
          </div>

          <div
            className={`flex items-start gap-3 rounded border px-3 py-3 ${
              isDeleted
                ? "border-emerald-200 bg-emerald-50 text-emerald-800"
                : errorMessage
                  ? "border-rose-300 bg-rose-50 text-rose-800"
                  : "border-sky-200 bg-sky-50 text-sky-800"
            }`}
          >
            <span className={`material-symbols-outlined mt-0.5 text-[18px] ${isDeleting ? "animate-spin" : ""}`}>{errorMessage ? "error" : statusIcon}</span>
            <div>
              <p className="text-sm font-semibold">{errorMessage ? "Nie udało się usunąć joba" : isDeleted ? "Usunięto" : isDeleting ? "Usuwanie w toku" : "Gotowe do usunięcia"}</p>
              <p className="mt-1 text-sm">{errorMessage ?? statusText}</p>
            </div>
          </div>
        </div>

        <div className="flex flex-col-reverse gap-2 border-t border-outline bg-surface px-5 py-4 sm:flex-row sm:justify-end">
          <button
            type="button"
            onClick={onCancel}
            disabled={isDeleting || isDeleted}
            className="inline-flex items-center justify-center gap-1 rounded border border-outline bg-white px-3 py-2 text-sm font-medium text-ink transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
          >
            Anuluj
          </button>
          <button
            type="button"
            onClick={onConfirm}
            disabled={isDeleting || isDeleted}
            className="inline-flex items-center justify-center gap-1 rounded border border-rose bg-rose px-3 py-2 text-sm font-semibold text-white shadow-sm transition hover:bg-[#991b1b] disabled:cursor-not-allowed disabled:opacity-60"
          >
            <span className={`material-symbols-outlined text-[16px] ${isDeleting ? "animate-spin" : ""}`}>{isDeleting ? "progress_activity" : "delete"}</span>
            Usuń job
          </button>
        </div>
      </div>
    </div>
  );
}

function DeleteScopeItem({ icon, label }: { icon: string; label: string }) {
  return (
    <div className="flex items-center gap-2">
      <span className="material-symbols-outlined text-[16px] text-steel">{icon}</span>
      <span>{label}</span>
    </div>
  );
}
