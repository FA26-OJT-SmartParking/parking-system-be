# Frontend

Placeholder for the web app. The FE team scaffolds one React + Three.js application here for customers, lot owners, staff and admins (see `docs/huong-dan-setup-microservices.md`, section 2):

```
npm create vite@latest . -- --template react-ts
npm install three
```

During development the app runs on `http://localhost:5173` and calls the gateway at `http://localhost:8088` (CORS already allows this origin). Real-time slot updates come from the SignalR hub at `/hubs/parking` (event `slotStatusChanged`).
