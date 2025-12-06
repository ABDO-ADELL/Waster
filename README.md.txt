# Waster API - Food Waste Reduction Platform

A comprehensive ASP.NET Core Web API for managing food donations and reducing waste by connecting food donors with recipients.

## 📋 Table of Contents

- [Features](#features)
- [Technologies Used](#technologies-used)
- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Configuration](#configuration)
- [Database Setup](#database-setup)
- [Running the Application](#running-the-application)
- [API Documentation](#api-documentation)
- [Project Structure](#project-structure)
- [Authentication](#authentication)
- [Real-time Notifications](#real-time-notifications)
- [Contributing](#contributing)
- [License](#license)

## ✨ Features

### Core Functionality
- **User Management**: Registration, login, profile management with JWT authentication
- **Google OAuth Integration**: Sign in with Google support
- **Post Management**: Create, update, delete, and browse food donation posts
- **Claiming System**: Users can claim available food posts
- **Bookmarking**: Save favorite posts for later
- **Real-time Notifications**: SignalR-powered instant notifications for claims and updates
- **Dashboard Analytics**: Track donations, claims, and impact metrics
- **Image Upload**: Support for food item images with file storage
- **Search & Filter**: Advanced search by category, location, and expiry date

### Security Features
- JWT token-based authentication
- Refresh token mechanism
- Role-based authorization
- Secure password hashing with ASP.NET Identity
- CORS configuration

## 🛠 Technologies Used

- **Framework**: ASP.NET Core 9.0
- **Database**: SQL Server with Entity Framework Core 9.0
- **Authentication**: ASP.NET Core Identity, JWT Bearer tokens
- **OAuth**: Google Sign-In integration
- **Real-time Communication**: SignalR
- **API Documentation**: Swagger/OpenAPI with Scalar UI
- **Architecture Patterns**: Repository Pattern, Unit of Work Pattern
- **Image Storage**: File system storage service
- **Phone Validation**: libphonenumber-csharp

## 📦 Prerequisites

Before you begin, ensure you have the following installed:

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (Express or higher)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)
- [Git](https://git-scm.com/)

## 🚀 Installation

### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/waster-api.git
cd waster-api
```

### 2. Restore NuGet Packages

```bash
dotnet restore
```

## ⚙️ Configuration

### 1. Update `appsettings.json`

Create an `appsettings.json` file in the root directory with the following structure:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Jwt": {
    "Key": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "WasterAPI",
    "Audience": "WasterClient",
    "DurationInDays": 30
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=WasterDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Authentication": {
    "Google": {
      "ClientId": "your-google-client-id.apps.googleusercontent.com",
      "ClientSecret": "your-google-client-secret"
    }
  },
  "AllowedOrigins": [
    "http://localhost:3000",
    "http://localhost:4200"
  ]
}
```

### 2. Configure Connection String

Update the `DefaultConnection` in `appsettings.json` with your SQL Server details:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER;Database=WasterDB;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
}
```

### 3. Google OAuth Setup (Optional)

To enable Google Sign-In:

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select existing
3. Enable Google+ API
4. Create OAuth 2.0 credentials
5. Add authorized redirect URIs: `https://localhost:5001/signin-google`
6. Copy Client ID and Client Secret to `appsettings.json`

## 🗄 Database Setup

### 1. Apply Migrations

```bash
dotnet ef database update
```

### 2. Seed Initial Data (Optional)

You can add roles and test users manually through the API endpoints or by creating a seed method.

## ▶️ Running the Application

### Development Mode

```bash
dotnet run
```

Or press `F5` in Visual Studio.

The API will be available at:
- HTTPS: `https://localhost:5001`
- HTTP: `http://localhost:5000`

### Access API Documentation

Navigate to:
- **Scalar UI**: `https://localhost:5001/scalar/v1`
- **Swagger UI**: `https://localhost:5001/swagger`

## 📚 API Documentation

### Main Endpoints

#### Authentication
- `POST /api/Authentication/Register` - Register new user
- `POST /api/Authentication/Login` - Login with credentials
- `POST /api/Authentication/RefreshToken` - Refresh access token
- `POST /api/Authentication/RevokeToken` - Revoke refresh token
- `POST /api/GoogleAuth/google-signin` - Sign in with Google

#### User Account
- `GET /api/Account/me` - Get current user profile
- `PUT /api/Account/update-name` - Update user name
- `PUT /api/Account/update-Location` - Update address
- `PUT /api/Account/update-PhoneNumber` - Update phone number
- `POST /api/Account/change-password` - Change password
- `POST /api/Account/change-email` - Change email
- `DELETE /api/Account/Delete-Account` - Delete account

#### Posts
- `POST /api/Post/Create-Post` - Create new food post
- `PUT /api/Post/Edit-post` - Update existing post
- `DELETE /api/Post/Delete-Post` - Delete post

#### Browse
- `GET /api/Browse/feed` - Get random feed of available posts
- `GET /api/Browse/expiring-soon` - Get posts expiring soon
- `GET /api/Browse/search` - Search posts with filters
- `GET /api/Browse/categories` - Get available categories

#### Claims
- `POST /api/ClaimPost/post/{postId}` - Claim a post
- `GET /api/ClaimPost/my-claims` - Get user's claims
- `GET /api/ClaimPost/post/{postId}/claims` - Get claims for a post (owner only)
- `PUT /api/ClaimPost/{claimId}/approve` - Approve claim
- `PUT /api/ClaimPost/{claimId}/reject` - Reject claim
- `PUT /api/ClaimPost/{claimId}/complete` - Mark claim as completed
- `DELETE /api/ClaimPost/{claimId}/cancel` - Cancel claim

#### Bookmarks
- `GET /api/BookMarks` - Get user's bookmarks
- `POST /api/BookMarks/{postId}` - Bookmark a post
- `DELETE /api/BookMarks/{postId}` - Remove bookmark
- `GET /api/BookMarks/check/{postId}` - Check if post is bookmarked

#### Notifications
- `GET /api/Notifications` - Get all notifications
- `GET /api/Notifications/unread-count` - Get unread count
- `PUT /api/Notifications/{id}/mark-read` - Mark as read
- `PUT /api/Notifications/mark-all-read` - Mark all as read

#### Dashboard
- `GET /api/Dashboard` - Get dashboard statistics
- `GET /api/Dashboard/my-stats` - Get personal statistics
- `GET /api/Dashboard/categories` - Get category statistics

### SignalR Hub

**Connection**: `/notificationHub`

Connect to receive real-time notifications for claims and updates.

## 📁 Project Structure

```
Waster/
├── Controllers/           # API Controllers
├── Models/               # Data models and entities
│   └── DbModels/        # Database models
├── DTOs/                # Data Transfer Objects
├── Services/            # Business logic services
├── Helpers/             # Helper classes and extensions
├── Hubs/                # SignalR hubs
├── Migrations/          # EF Core migrations
├── wwwroot/            # Static files
│   └── uploads/        # Uploaded images
├── Program.cs          # Application entry point
├── AppDbContext.cs     # Database context
└── appsettings.json    # Configuration
```

## 🔐 Authentication

### JWT Token Authentication

The API uses JWT tokens for authentication. Include the token in the Authorization header:

```
Authorization: Bearer <your-token>
```

### Token Lifecycle

1. **Login/Register**: Receive access token and refresh token
2. **Access Token**: Valid for 30 days (configurable)
3. **Refresh Token**: Valid for 7 days, used to get new access token
4. **Token Refresh**: Use `/api/Authentication/RefreshToken` to get new tokens
5. **Logout**: Revoke refresh token using `/api/Authentication/RevokeToken`

## 🔔 Real-time Notifications

The application uses SignalR for real-time notifications:

1. Connect to `/notificationHub` with authentication
2. Listen for `ReceiveNotification` events
3. Receive instant updates for:
   - New claims on your posts
   - Claim approvals
   - Claim rejections

## 🤝 Contributing

Contributions are welcome! Please follow these steps:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 👥 Authors

- Your Name - Initial work

## 🙏 Acknowledgments

- ASP.NET Core team for the excellent framework
- All contributors and testers

## 📞 Support

For support, email support@wasterapi.com or open an issue in the repository.

---

**Note**: Remember to never commit sensitive information like connection strings, API keys, or secrets to version control. Use User Secrets for development and environment variables for production.