const express = require("express");

const app = express();
const port = process.env.PORT || 3000;

app.get("/", (_req, res) => {
  res.json({ ok: true });
});

app.listen(port, "0.0.0.0");
