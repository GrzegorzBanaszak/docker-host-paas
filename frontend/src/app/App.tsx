import { Navigate, Route, Routes } from "react-router-dom";
import { AppShell } from "../components/AppShell";
import { DashboardPage } from "../pages/DashboardPage";
import { ImageDetailsPage } from "../pages/ImageDetailsPage";
import { ImagesPage } from "../pages/ImagesPage";
import { JobCreatePage } from "../pages/JobCreatePage";
import { JobDetailsPage } from "../pages/JobDetailsPage";
import { JobsPage } from "../pages/JobsPage";

export function App() {
  return (
    <AppShell>
      <Routes>
        <Route path="/" element={<DashboardPage />} />
        <Route path="/jobs" element={<JobsPage />} />
        <Route path="/jobs/new" element={<JobCreatePage />} />
        <Route path="/jobs/:jobId" element={<JobDetailsPage />} />
        <Route path="/images" element={<ImagesPage />} />
        <Route path="/images/:imageId" element={<ImageDetailsPage />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </AppShell>
  );
}
