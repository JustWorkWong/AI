import { createRouter, createWebHistory } from "vue-router";
import DashboardPage from "./pages/DashboardPage.vue";
import ReturnWorkbenchPage from "./pages/ReturnWorkbenchPage.vue";
import SopAssistPage from "./pages/SopAssistPage.vue";

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: "/", component: DashboardPage },
    { path: "/returns/:id", component: ReturnWorkbenchPage },
    { path: "/sop/:sessionId", component: SopAssistPage }
  ]
});
