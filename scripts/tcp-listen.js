#!/usr/bin/env node
const net = require("net");

const args = process.argv.slice(2);
const HOST = "0.0.0.0";
const PORT = parseInt(args.shift()) || 5999;

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
