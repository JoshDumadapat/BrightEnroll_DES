# BrightEnroll DES - Enrollment Management System

A .NET MAUI Blazor application for managing student enrollment, academics, finance, and human resources.

## Prerequisites

- Visual Studio 2022 (17.8 or later) with .NET MAUI workload
- .NET 9.0 SDK
- Node.js (v16 or later) and npm (for Tailwind CSS)

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/JoshDumadapat/BrightEnroll_DES.git
cd BrightEnroll_DES/BrightEnroll_DES
```

### 2. Restore Dependencies

#### .NET Dependencies
The .NET dependencies will be automatically restored when you open the project in Visual Studio. Alternatively, you can run:

```bash
dotnet restore
```

#### Node.js Dependencies (for Tailwind CSS)
```bash
npm install
```

### 3. Build Tailwind CSS

The Tailwind CSS will be automatically built during the project build process. However, if you need to build it manually:

```bash
npm run build-css
```

For development with watch mode:

```bash
npm run watch-css
```

### 4. Open in Visual Studio

1. Open Visual Studio 2022
2. File → Open → Project/Solution
3. Navigate to the cloned repository folder
4. Select `BrightEnroll_DES.csproj` file
5. Visual Studio will automatically restore NuGet packages and build the project

### 5. Run the Application

- Press `F5` or click the Run button in Visual Studio
- Select your target platform (Windows, Android, iOS, etc.)
- The application will build and launch

## Project Structure

```
BrightEnroll_DES/
├── Components/
│   ├── Layout/          # Layout components (MainLayout, AuthLayout, etc.)
│   └── Pages/           # Page components
│       ├── Admin/       # Admin pages (Dashboard, Academic, Finance, etc.)
│       └── Auth/        # Authentication pages
├── Services/            # Business logic services
├── Models/             # Data models
├── wwwroot/            # Static web assets
│   ├── css/           # CSS files (including Tailwind)
│   ├── js/            # JavaScript files
│   └── images/        # Image assets
└── Platforms/         # Platform-specific code

```

## Features

- **Student Management**: Registration, records, and academic tracking
- **Academic Management**: Sections, subjects, and teacher assignments
- **Finance Management**: Fee setup, payments, and records
- **Human Resources**: Employee management and records
- **Enrollment**: New applicants, enrollment processing, and re-enrollment

## Technologies Used

- .NET MAUI 9.0
- Blazor
- Tailwind CSS 3.4.1
- C# 12

## Notes

- The project uses Tailwind CSS for styling, which is automatically built during the build process
- Make sure Node.js is installed for Tailwind CSS compilation
- The solution file is not included as .NET MAUI projects can be opened directly via the .csproj file in Visual Studio

## Troubleshooting

### Build Errors

1. **Tailwind CSS not building**: Make sure Node.js is installed and run `npm install`
2. **NuGet package errors**: Run `dotnet restore` in the project directory
3. **Platform-specific errors**: Ensure you have the required platform SDKs installed

### Visual Studio Issues

- If the project doesn't load, make sure you have the .NET MAUI workload installed in Visual Studio
- Go to: Tools → Get Tools and Features → Individual Components → .NET MAUI

## License

[Add your license information here]

