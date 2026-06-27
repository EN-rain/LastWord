// godot-mcp-wrapper.cjs — MCP server wrapper that sets GODOT_PATH before
// starting the actual @coding-solo/godot-mcp server.
//
// Loads the server from its Kimchi global install path.

process.env.GODOT_PATH = "C:\\Users\\LENOVO\\Desktop\\Godot_v4.6.2-stable_mono_win64\\Godot_v4.6.2-stable_mono_win64.exe";
process.env.DEBUG = "true";

// Explicit path to the Kimchi-installed global package
require("C:/Users/LENOVO/AppData/Roaming/npm/node_modules/@coding-solo/godot-mcp/build/index.js");
