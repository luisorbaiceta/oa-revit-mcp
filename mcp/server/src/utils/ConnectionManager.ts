import { RevitClientConnection } from "./SocketClient.js";

// Create a single, shared RevitClientConnection instance
const revitClient = new RevitClientConnection("localhost", 8080);

/**
 * Returns a connected RevitClientConnection instance.
 * If the client is not connected, it will attempt to connect.
 * @returns A connected RevitClientConnection instance.
 */
export async function getRevitConnection(): Promise<RevitClientConnection> {
  // If the client is already connected, return it.
  if (revitClient.isConnected) {
    return revitClient;
  }

  // If not connected, attempt to connect with a timeout.
  await new Promise<void>((resolve, reject) => {
    const onConnect = () => {
      // Clear the timeout and remove listeners to prevent memory leaks
      clearTimeout(timeout);
      revitClient.socket.removeListener("connect", onConnect);
      revitClient.socket.removeListener("error", onError);
      resolve();
    };

    const onError = (error: any) => {
      // Clear the timeout and remove listeners
      clearTimeout(timeout);
      revitClient.socket.removeListener("connect", onConnect);
      revitClient.socket.removeListener("error", onError);
      reject(new Error("Failed to connect to Revit client"));
    };

    revitClient.socket.on("connect", onConnect);
    revitClient.socket.on("error", onError);

    revitClient.connect();

    // Set a timeout for the connection attempt.
    const timeout = setTimeout(() => {
      revitClient.socket.removeListener("connect", onConnect);
      revitClient.socket.removeListener("error", onError);
      reject(new Error("Connection to Revit client timed out after 5 seconds"));
    }, 5000);
  });

  return revitClient;
}
