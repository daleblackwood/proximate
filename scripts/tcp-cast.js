#!/usr/bin/env node
const net = require("net");

const HOST = process.argv[2] || "127.0.0.1";
const PORT = process.argv[3] ? parseInt(process.argv[3], 10) : 5999;
let count = 0;

const client = new net.Socket();

client.connect(PORT, HOST, () => {
    console.log(`Connected to server at ${HOST}:${PORT}`);
});

client.on("error", (err) => console.error("Client error:", err));

setInterval(() => {
    const message = JSON.stringify({ time: new Date().toISOString(), count });
    count++;
    client.write(message + "\n");
    console.log("Sent:", message);
}, 1000);
