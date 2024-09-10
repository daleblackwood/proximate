#!/usr/bin/env node
const net = require("net");

const HOST = process.argv[2] || "0.0.0.0";
const PORT = process.argv[3] ? parseInt(process.argv[3], 10) : 5999;

const server = net.createServer((socket) => {
    console.log(`Client connected: ${socket.remoteAddress}:${socket.remotePort}`);

    socket.on("data", (data) => {
        console.log(`Received: ${data.toString().trim()} from ${socket.remoteAddress}:${socket.remotePort}`);
    });

    socket.on("end", () => {
        console.log(`Client disconnected: ${socket.remoteAddress}:${socket.remotePort}`);
    });

    socket.on("error", (err) => console.error("Socket error:", err));
});

server.listen(PORT, HOST, () => {
    console.log(`Listening on ${HOST}:${PORT}...`);
});
