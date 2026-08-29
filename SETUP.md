# Setup Guide — Budget Grocery List (Blazor WebAssembly PWA)

## What this is

A single C# codebase that runs as:
- a **website** (any desktop or phone browser), and
- an **installable app** (via "Install app" / "Add to Home Screen" in the browser, thanks to the included PWA manifest + service worker)

No native Windows/Mac/iOS/Android build tools required — it's all standard web tech under the hood (WebAssembly), just written in C#/Razor instead of JavaScript.

## Prerequisites

- **Visual Studio 2022** (Community edition is free) with the **"ASP.NET and web development"** workload installed
  (Visual Studio Installer > Modify > tick that workload if you don't already have it)
- Or, if you prefer the command line: the **.NET 8 SDK** from https://dotnet.microsoft.com/download


## Run from the command line

```bash
dotnet restore
dotnet run
```

It will print a local URL (usually `https://localhost:7xxx` or `http://localhost:5xxx`) — open that in your browser.

## Testing the "installable app" behaviour

1. Run the app and open it in **Chrome** or **Edge**.
2. Look for an **install icon** in the address bar (or Menu > "Install Budget Grocery List...").
3. Click it — the app opens in its own window without browser tabs/address bar, like a real app.
4. On a **phone browser**, look for "Add to Home Screen" in the browser menu — same effect, gives you a home screen icon.

> Note: full offline caching and the installability prompt are most reliable in a **published/release** build (`dotnet publish -c Release`) served over **https**, rather than the plain dev server. For a quick class demo, the dev server (`dotnet run`) is normally sufficient to show the UI and install prompt.

## Project structure

```
GroceryBudgetWeb/
├── GroceryBudgetWeb.csproj
├── Program.cs                # App startup
├── App.razor                 # Root router
├── _Imports.razor            # Global using directives
├── Layout/
│   └── MainLayout.razor
├── Pages/
│   └── Home.razor            # The entire app UI + logic lives here
├── Models/
│   ├── GroceryItem.cs        # A preset catalog item
│   └── ListLineItem.cs       # A row in the user's list
└── wwwroot/
    ├── index.html            # HTML shell
    ├── manifest.json         # PWA manifest
    ├── service-worker.js     # Enables offline caching + installability
    ├── icon-192.png / icon-512.png / favicon.png
    ├── css/app.css           # All styling — edit this to restyle the app
    └── data/items.json       # Preset ~50-item dataset (id, name, category, price, emoji)
```
