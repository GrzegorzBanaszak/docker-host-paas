import { useState } from "react";
import { PageHeader } from "../components/PageHeader";
import { Panel } from "../components/Panel";
import { ImageCatalog } from "../features/images/ImageCatalog";
import { useImages } from "../features/jobs/hooks";

export function ImagesPage() {
  const imagesQuery = useImages();
  const [search, setSearch] = useState("");
  const images = (imagesQuery.data ?? []).filter((image) => {
    const haystack = [
      image.jobName,
      image.repositoryUrl,
      image.branch ?? "",
      image.status,
      image.imageTag ?? "",
      image.sourceCommitSha ?? ""
    ]
      .join(" ")
      .toLowerCase();

    return haystack.includes(search.trim().toLowerCase());
  });

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Artifacts"
        title="Image Registry"
        description="Browse built Docker images, inspect build snapshots, and remove obsolete artifacts."
      />

      <div className="rounded border border-outline bg-white p-3">
        <div className="relative w-full md:w-72">
          <span className="material-symbols-outlined absolute left-2 top-1/2 -translate-y-1/2 text-[16px] text-slate-400">
            search
          </span>
          <input
            className="h-9 w-full rounded border border-outline bg-white pl-8 pr-3 text-sm outline-none focus:border-sky"
            placeholder="Filter by job, repo, tag, commit..."
            type="text"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
          />
        </div>
      </div>

      <Panel>
        <ImageCatalog images={images} />
      </Panel>
    </div>
  );
}
