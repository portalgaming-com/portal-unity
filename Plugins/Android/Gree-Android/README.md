# Gree Android plugin

This is the library for the `WebViewPlugin.aar` that is used for Gree in Unity.

To build and use a new version: 
1. Run the `assemble` gradle command (A custom task will copy the generated aar to `src/Packages/Identity/Runtime/ThirdParty/Gree/Assets/Plugins/Android` and rename it to `WebViewPlugin.aar`)
2. Update `src/packages/Identity/Runtime/ThirdParty/Gree/Assets/PluginsWebViewObject.cs` if any references were changed
