import { Navigate, Route, Routes } from "react-router-dom";
import { AppShell } from "../components/AppShell";
import { DashboardPage } from "../pages/DashboardPage";
import { DnsPage } from "../pages/DnsPage";
import { ImageDetailsPage } from "../pages/ImageDetailsPage";
import { ImagesPage } from "../pages/ImagesPage";
import { JobCreatePage } from "../pages/JobCreatePage";
import { JobDetailsPage } from "../pages/JobDetailsPage";
import { JobsPage } from "../pages/JobsPage";
import { ProjectCreatePage } from "../pages/ProjectCreatePage";
import { ProjectDetailsPage } from "../pages/ProjectDetailsPage";
import { ProjectsPage } from "../pages/ProjectsPage";

export function App() {
  return (
    <AppShell>
      <Routes>
        <Route path="/" element={<DashboardPage />} />
        <Route path="/projects" element={<ProjectsPage />} />
        <Route path="/projects/new" element={<ProjectCreatePage />} />
        <Route path="/projects/:projectId" element={<ProjectDetailsPage />} />
        <Route path="/jobs" element={<JobsPage />} />
        <Route path="/jobs/new" element={<JobCreatePage />} />
        <Route path="/jobs/:jobId" element={<JobDetailsPage />} />
        <Route path="/dns" element={<DnsPage />} />
        <Route path="/images" element={<ImagesPage />} />
        <Route path="/images/:imageId" element={<ImageDetailsPage />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </AppShell>
  );
}
