import { RevitClientConnection } from "../utils/SocketClient.js";
import { jest } from "@jest/globals";

// Define a mock socket instance that we can reference in tests
const mockSocketInstance = {
  on: jest.fn(),
  connect: jest.fn(),
  write: jest.fn(),
  end: jest.fn(),
  removeListener: jest.fn(),
};

// Mock the 'net' module
jest.mock("net", () => ({
  Socket: jest.fn().mockImplementation(() => mockSocketInstance),
}));

describe("RevitClientConnection", () => {
  let client: RevitClientConnection;

  beforeEach(() => {
    // Clear all mocks before each test
    jest.clearAllMocks();
    // Create a new client before each test
    client = new RevitClientConnection("localhost", 8080);
  });

  describe("processBuffer", () => {
    it("should process a single complete JSON object", () => {
      const handleResponse = jest.spyOn(client as any, "handleResponse");
      const json = '{"id":"1","result":"success"}';
      client.buffer = json;
      (client as any).processBuffer();
      expect(handleResponse).toHaveBeenCalledWith(json);
      expect(client.buffer).toBe("");
    });

    it("should process multiple complete JSON objects", () => {
      const handleResponse = jest.spyOn(client as any, "handleResponse");
      const json1 = '{"id":"1","result":"success"}';
      const json2 = '{"id":"2","result":"another success"}';
      client.buffer = json1 + json2;
      (client as any).processBuffer();
      expect(handleResponse).toHaveBeenCalledWith(json1);
      expect(handleResponse).toHaveBeenCalledWith(json2);
      expect(client.buffer).toBe("");
    });

    it("should not process an incomplete JSON object", () => {
      const handleResponse = jest.spyOn(client as any, "handleResponse");
      const json = '{"id":"1","result":"';
      client.buffer = json;
      (client as any).processBuffer();
      expect(handleResponse).not.toHaveBeenCalled();
      expect(client.buffer).toBe(json);
    });

    it("should handle a mix of complete and incomplete objects", () => {
      const handleResponse = jest.spyOn(client as any, "handleResponse");
      const json1 = '{"id":"1","result":"success"}';
      const partialJson = '{"id":"2","result":"';
      client.buffer = json1 + partialJson;
      (client as any).processBuffer();
      expect(handleResponse).toHaveBeenCalledWith(json1);
      expect(handleResponse).toHaveBeenCalledTimes(1);
      expect(client.buffer).toBe(partialJson);
    });
  });

  describe("sendCommand", () => {
    it("should send a command and handle a response", async () => {
      const command = "test_command";
      const params = { foo: "bar" };
      const requestId = "12345";
      const responseJson = { id: requestId, result: "it worked" };

      // Mock generateRequestId to return a predictable ID
      jest
        .spyOn(client as any, "generateRequestId")
        .mockReturnValue(requestId);

      client.isConnected = true; // Pretend we are connected
      const promise = client.sendCommand(command, params);

      // Find the response callback and simulate a response
      const callback = client.responseCallbacks.get(requestId);
      expect(callback).toBeDefined();
      callback!(JSON.stringify(responseJson));

      const result = await promise;
      expect(result).toBe("it worked");
      expect(mockSocketInstance.write).toHaveBeenCalledWith(
        expect.stringContaining('"method":"test_command"')
      );
    });
  });
});
