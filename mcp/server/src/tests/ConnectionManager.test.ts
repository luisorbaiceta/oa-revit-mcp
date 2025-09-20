import { jest } from "@jest/globals";

describe("ConnectionManager", () => {
  let getRevitConnection: () => Promise<any>;
  let RevitClientConnection: jest.Mock;
  let mockClientInstance: any;

  beforeEach(async () => {
    jest.resetModules();

    // Mock the RevitClientConnection before importing any other module
    jest.mock("../utils/SocketClient", () => ({
      RevitClientConnection: jest.fn().mockImplementation(() => {
        // This is the mock instance created by the singleton
        mockClientInstance = {
          isConnected: false,
          socket: {
            on: jest.fn(),
            removeListener: jest.fn(),
          },
          connect: jest.fn(),
        };
        return mockClientInstance;
      }),
    }));

    // Now, import the modules
    const connectionManagerModule = await import("../utils/ConnectionManager");
    getRevitConnection = connectionManagerModule.getRevitConnection;

    const socketClientModule = await import("../utils/SocketClient");
    RevitClientConnection =
      socketClientModule.RevitClientConnection as jest.Mock;
  });

  // Helper function to simulate a successful connection
  const simulateConnect = () => {
    // Find the on('connect', cb) call and invoke the callback
    const onConnect = mockClientInstance.socket.on.mock.calls.find(
      (call: [string, Function]) => call[0] === "connect"
    )[1];
    onConnect();
  };

  // Helper function to simulate a failed connection
  const simulateError = () => {
    // Find the on('error', cb) call and invoke the callback
    const onError = mockClientInstance.socket.on.mock.calls.find(
      (call: [string, Function]) => call[0] === "error"
    )[1];
    onError();
  };

  it("should create one instance of RevitClientConnection on the first call", async () => {
    const promise = getRevitConnection();
    simulateConnect(); // Allow the promise to resolve
    await promise;
    expect(RevitClientConnection).toHaveBeenCalledTimes(1);
  });

  it("should not create a new instance on subsequent calls", async () => {
    // First call
    let promise = getRevitConnection();
    simulateConnect();
    await promise;

    // Second call
    mockClientInstance.isConnected = true; // Mark as connected
    await getRevitConnection();

    expect(RevitClientConnection).toHaveBeenCalledTimes(1);
  });

  it("should call connect() if not connected", async () => {
    const promise = getRevitConnection();
    simulateConnect();
    await promise;
    expect(mockClientInstance.connect).toHaveBeenCalledTimes(1);
  });

  it("should not call connect() if already connected", async () => {
    mockClientInstance.isConnected = true;
    await getRevitConnection();
    expect(mockClientInstance.connect).not.toHaveBeenCalled();
  });

  it("should handle connection errors", async () => {
    const promise = getRevitConnection();
    simulateError(); // Simulate a connection error
    await expect(promise).rejects.toThrow("Failed to connect to Revit client");
  });

  it("should handle connection timeout", async () => {
    jest.useFakeTimers();
    const promise = getRevitConnection();
    // Fast-forward time to trigger the timeout
    jest.runAllTimers();
    await expect(promise).rejects.toThrow(
      "Connection to Revit client timed out after 5 seconds"
    );
    jest.useRealTimers();
  });
});
