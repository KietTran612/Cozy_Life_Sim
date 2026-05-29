import os
import sys

# Ensure realvirtual MCP python library is in sys.path
mcp_lib_path = "D:/soflware/Unity/Source/Cozy_Life_Sim/Assets/StreamingAssets/realvirtual-MCP/Lib"
if mcp_lib_path not in sys.path:
    sys.path.append(mcp_lib_path)

import asyncio
import json
import websockets

async def call_tool(tool_name, arguments=None):
    if arguments is None:
        arguments = {}
    uri = "ws://127.0.0.1:18711/mcp"
    try:
        async with websockets.connect(uri) as websocket:
            payload = {
                "command": "__call__",
                "tool": tool_name,
                "arguments": arguments
            }
            await websocket.send(json.dumps(payload))
            response = await websocket.recv()
            data = json.loads(response)
            if "error" in data:
                print(f"Error: {data['error']}", file=sys.stderr)
                sys.exit(1)
            else:
                print(json.dumps(data.get("result", {}), indent=2))
    except Exception as e:
        print(f"Connection failed: {e}", file=sys.stderr)
        sys.exit(1)

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python run_unity_mcp.py <tool_name> [key=value key2=value2 ...]")
        sys.exit(1)
    
    tool = sys.argv[1]
    args = {}
    for arg in sys.argv[2:]:
        if "=" in arg:
            k, v = arg.split("=", 1)
            # Try to parse as int or bool if applicable
            if v.lower() == "true":
                v = True
            elif v.lower() == "false":
                v = False
            else:
                try:
                    if "." in v:
                        v = float(v)
                    else:
                        v = int(v)
                except ValueError:
                    pass
            args[k] = v
        else:
            # Fallback to loading whole arg as json
            try:
                args = json.loads(arg)
            except json.JSONDecodeError:
                pass
    
    asyncio.run(call_tool(tool, args))
