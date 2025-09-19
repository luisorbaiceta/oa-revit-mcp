import * as net from "net";

export class RevitClientConnection {
  host: string;
  port: number;
  socket: net.Socket;
  isConnected: boolean = false;
  responseCallbacks: Map<string, (response: string) => void> = new Map();
  buffer: string = "";

  constructor(host: string, port: number) {
    this.host = host;
    this.port = port;
    this.socket = new net.Socket();
    this.setupSocketListeners();
  }

  private setupSocketListeners(): void {
    this.socket.on("connect", () => {
      this.isConnected = true;
    });

    this.socket.on("data", (data) => {
      // 将接收到的数据添加到缓冲区
      const dataString = data.toString();
      this.buffer += dataString;

      // 尝试解析完整的JSON响应
      this.processBuffer();
    });

    this.socket.on("close", () => {
      this.isConnected = false;
    });

    this.socket.on("error", (error) => {
      console.error("RevitClientConnection error:", error);
      this.isConnected = false;
    });
  }

  private processBuffer(): void {
    let braceCount = 0;
    let lastIndex = 0;

    for (let i = 0; i < this.buffer.length; i++) {
      if (this.buffer[i] === "{") {
        braceCount++;
      } else if (this.buffer[i] === "}") {
        braceCount--;
      }

      if (braceCount === 0 && i > lastIndex) {
        const jsonString = this.buffer.substring(lastIndex, i + 1);
        try {
          // We have a complete JSON object
          this.handleResponse(jsonString);
          // Move lastIndex to the start of the next potential JSON object
          lastIndex = i + 1;
        } catch (error) {
          // This could happen if the substring is not a valid JSON object,
          // which might indicate a problem with the data stream or our parsing logic.
          // For now, we'll log the error and continue, which will discard the malformed segment.
          console.error("Error parsing JSON object from buffer:", error);
          console.error("Malformed JSON string:", jsonString);
          // Move lastIndex to avoid getting stuck on this segment
          lastIndex = i + 1;
        }
      }
    }

    // Update the buffer to remove the processed parts
    if (lastIndex > 0) {
      this.buffer = this.buffer.substring(lastIndex);
    }
  }

  public connect(): boolean {
    if (this.isConnected) {
      return true;
    }

    try {
      this.socket.connect(this.port, this.host);
      return true;
    } catch (error) {
      console.error("Failed to connect:", error);
      return false;
    }
  }

  public disconnect(): void {
    this.socket.end();
    this.isConnected = false;
  }

  private generateRequestId(): string {
    return Date.now().toString() + Math.random().toString().substring(2, 8);
  }

  private handleResponse(responseData: string): void {
    try {
      const response = JSON.parse(responseData);
      // 从响应中获取ID
      const requestId = response.id || "default";

      const callback = this.responseCallbacks.get(requestId);
      if (callback) {
        callback(responseData);
        this.responseCallbacks.delete(requestId);
      }
    } catch (error) {
      console.error("Error parsing response:", error);
    }
  }

  public sendCommand(command: string, params: any = {}): Promise<any> {
    return new Promise((resolve, reject) => {
      try {
        if (!this.isConnected) {
          this.connect();
        }

        // 生成请求ID
        const requestId = this.generateRequestId();

        // 创建符合JSON-RPC标准的请求对象
        const commandObj = {
          jsonrpc: "2.0",
          method: command,
          params: params,
          id: requestId,
        };

        // 存储回调函数
        this.responseCallbacks.set(requestId, (responseData) => {
          try {
            const response = JSON.parse(responseData);
            if (response.error) {
              reject(
                new Error(response.error.message || "Unknown error from Revit")
              );
            } else {
              resolve(response.result);
            }
          } catch (error) {
            if (error instanceof Error) {
              reject(new Error(`Failed to parse response: ${error.message}`));
            } else {
              reject(new Error(`Failed to parse response: ${String(error)}`));
            }
          }
        });

        // 发送命令
        const commandString = JSON.stringify(commandObj);
        this.socket.write(commandString);

        // 设置超时
        setTimeout(() => {
          if (this.responseCallbacks.has(requestId)) {
            this.responseCallbacks.delete(requestId);
            reject(new Error(`Command timed out after 2 minutes: ${command}`));
          }
        }, 120000); // 2分钟超时
      } catch (error) {
        reject(error);
      }
    });
  }
}
