# BioAuth System

> A desktop biometric authentication system built with WPF, OpenCV, and SQL Server.

BioAuth System is a facial recognition and authentication application developed for Windows. The project combines real-time webcam processing, face detection, biometric enrollment, and database-backed user management into a single desktop application.

Users can create an account, enroll their face through the camera, and authenticate using facial recognition.

The system is built around the **LBPH (Local Binary Patterns Histograms)** face recognition algorithm using **OpenCvSharp**, with live video capture handled through **AForge.NET** and the application interface built with **WPF on .NET 8**.

---

## Overview

The project follows a simple biometric authentication workflow:

```text
User Registration
        │
        ▼
Face Enrollment
        │
        ▼
Multiple Face Samples
        │
        ▼
Face Processing
        │
        ▼
SQL Server Storage
        │
        ▼
Real-Time Face Recognition
        │
        ▼
Authentication Result
```

Instead of relying only on a traditional username and password, the application adds biometric facial data as part of the authentication process.

---

## Features

### Face Enrollment

The system captures multiple facial samples during the registration process.

Each frame is processed to:

* Detect the face
* Extract the face region
* Convert the image to grayscale
* Normalize the image size
* Prepare the sample for recognition

Multiple samples help provide variation in facial position and improve the recognition process.

### Real-Time Face Recognition

The application processes webcam frames in real time.

Detected faces are compared against the biometric data stored in the database using an **LBPH Face Recognizer**.

The recognition result is evaluated using a confidence or distance threshold.

```text
Lower Distance → Better Match
Higher Distance → Weaker Match
```

A configurable threshold determines whether authentication is accepted or rejected.

### Face Detection with Haar Cascade

Face detection is handled using OpenCV's Haar Cascade classifier.

The system can use parameters such as:

```csharp id="bc57re"
scaleFactor: 1.1
minNeighbors: 5
minSize: new Size(60, 60)
```

These values help balance detection speed and accuracy.

### WPF Desktop Interface

The application uses WPF to provide a modern Windows desktop interface.

The UI includes:

* User registration
* Face enrollment
* Live camera preview
* Facial authentication
* Application navigation
* Animated interface elements

### SQL Server Integration

User information and facial image data are stored in Microsoft SQL Server.

The biometric images are stored as binary data using:

```sql id="aj9xoy"
VARBINARY(MAX)
```

This allows the application to manage biometric samples directly through the database rather than depending on external image file paths.

### Resource Management

The application handles camera streams and OpenCV resources carefully to reduce unnecessary memory usage during continuous video processing.

---

## Tech Stack

| Technology                  | Purpose                              |
| --------------------------- | ------------------------------------ |
| **.NET 8**                  | Application framework                |
| **WPF**                     | Desktop user interface               |
| **C#**                      | Core application logic               |
| **OpenCvSharp4**            | Computer vision and image processing |
| **OpenCvSharp4.Extensions** | OpenCV and .NET image integration    |
| **LBPHFaceRecognizer**      | Facial recognition                   |
| **Haar Cascade**            | Face detection                       |
| **AForge.Video.DirectShow** | Webcam access and video streaming    |
| **Microsoft SQL Server**    | User and biometric data storage      |
| **XAML**                    | Interface design and animations      |

---

## System Architecture

The application consists of several connected layers:

```text
┌─────────────────────────────┐
│        WPF Interface        │
│ Registration / Login / UI   │
└──────────────┬──────────────┘
               │
               ▼
┌─────────────────────────────┐
│     Camera Processing       │
│  AForge + OpenCV Pipeline   │
└──────────────┬──────────────┘
               │
               ▼
┌─────────────────────────────┐
│       Face Detection        │
│        Haar Cascade         │
└──────────────┬──────────────┘
               │
               ▼
┌─────────────────────────────┐
│      Face Recognition       │
│     LBPHFaceRecognizer      │
└──────────────┬──────────────┘
               │
               ▼
┌─────────────────────────────┐
│         SQL Server          │
│ Users + Biometric Samples   │
└─────────────────────────────┘
```

---

## Database Structure

The system uses two main entities:

### Users

Stores the primary user information.

```sql id="ylh7hy"
CREATE TABLE Users (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(100) NOT NULL,
    Username NVARCHAR(50) UNIQUE NOT NULL,
    Password NVARCHAR(255) NOT NULL,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME DEFAULT GETDATE()
);
```

### User Faces

Stores the biometric face samples associated with each user.

```sql id="f01cdy"
CREATE TABLE Details (
    DetailID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL,
    ImagePath VARBINARY(MAX) NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),

    FOREIGN KEY (UserID)
        REFERENCES Users(UserID)
        ON DELETE CASCADE
);
```

The relationship can be represented as:

```text
Users
  │
  │ 1
  │
  └───────────< Many
                   │
                   ▼
                Details
            Face Samples
```

A single user can have multiple facial samples stored for training and recognition.

---

## Face Processing Pipeline

During face enrollment, the camera stream passes through the following stages:

```text
Webcam Frame
      │
      ▼
Face Detection
      │
      ▼
Face Region Extraction
      │
      ▼
Grayscale Conversion
      │
      ▼
Histogram Equalization
      │
      ▼
Image Resize
      │
      ▼
100 × 100 Face Sample
      │
      ▼
Database Storage
```

The processed face samples can be normalized to:

```text
100 × 100 pixels
```

Histogram equalization can also be applied using:

```csharp id="5bq1v2"
Cv2.EqualizeHist(source, destination);
```

This helps normalize image contrast before recognition.

---

## Authentication Workflow

The authentication process works as follows:

```text
Camera Input
     │
     ▼
Detect Face
     │
     ▼
Crop Face Region
     │
     ▼
Convert to Grayscale
     │
     ▼
Normalize Image
     │
     ▼
LBPH Recognition
     │
     ▼
Confidence Evaluation
     │
     ├── Match Accepted
     │
     └── Match Rejected
```

The recognition threshold can be configured depending on the quality of the dataset and camera conditions.

An example threshold might be:

```csharp id="j9zfsd"
const double RecognitionThreshold = 80;
```

In general:

```text
Distance < Threshold  → Authentication Accepted
Distance ≥ Threshold  → Authentication Rejected
```

The exact threshold should be tested and calibrated using real enrollment data.

---

## Getting Started

### Requirements

Before running the project, make sure the following are installed:

* Windows 10 or Windows 11
* Visual Studio 2022
* .NET Desktop Development workload
* .NET 8 SDK
* Microsoft SQL Server or SQL Server Express
* SQL Server Management Studio
* A webcam

---

## Installation

### Clone the Repository

**HTTPS**

```bash id="6nygl9"
git clone https://github.com/enzo963/BiometricAuthenticationSystem.git
cd BiometricAuthenticationSystem
```

**SSH**

```bash id="1cnzo7"
git clone git@github.com:enzo963/BiometricAuthenticationSystem.git
```

**GitHub CLI**

```bash id="8et8zu"
gh repo clone enzo963/BiometricAuthenticationSystem
```

---

## Database Setup

1. Open **SQL Server Management Studio**.

2. Create a new database:

```sql id="srt2es"
CREATE DATABASE BioAuthDB;
```

3. Select the database:

```sql id="0lcrv9"
USE BioAuthDB;
```

4. Create the `Users` and `Details` tables using the schema above.

5. Configure the connection string according to your SQL Server instance.

Example:

```csharp id="0koyge"
Data Source=YOUR_SERVER;
Initial Catalog=BioAuthDB;
Integrated Security=True;
TrustServerCertificate=True;
```

For SQL Server Express:

```text
YOUR-PC\SQLEXPRESS
```

---

## Face Detection Resource

The project requires the Haar Cascade XML file:

```text
haarcascade_frontalface_default.xml
```

Place it inside:

```text
Resources/
└── haarcascade_frontalface_default.xml
```

Make sure the file is copied to the output directory when building the application.

---

## Running the Application

Open the solution file in Visual Studio:

```text
BiometricAuthenticationSystem.sln
```

Then:

1. Restore the NuGet packages.
2. Verify the SQL Server connection.
3. Confirm that the Haar Cascade file is available.
4. Connect a webcam.
5. Build the project.
6. Press `F5`.

---

## Project Structure

The structure may vary depending on the current implementation, but the project is generally organized around the following components:

```text
BiometricAuthenticationSystem/
│
├── Resources/
│   └── haarcascade_frontalface_default.xml
│
├── Windows/
│   ├── RegistrationWindow.xaml
│   ├── SaveYourFace.xaml
│   ├── LoginWindow.xaml
│   ├── LoginFaceWindow.xaml
│   └── DashboardWindow.xaml
│
├── Services/
│   ├── Database/
│   ├── FaceDetection/
│   └── FaceRecognition/
│
├── App.xaml
├── App.xaml.cs
└── BiometricAuthenticationSystem.sln
```

---

## Configuration Notes

### Database Naming Consistency

The project should use consistent naming for the biometric table and image column.

For example:

```text
Table: Details
Column: ImagePath
```

or:

```text
Table: UserFaces
Column: FaceData
```

Both naming styles should not be mixed across different application components.

A single schema should be used throughout the project.

### Centralized Connection String

Instead of repeating the connection string in multiple classes, it is recommended to centralize the database configuration.

For example:

```text
App.config
```

or:

```text
DbConfig.cs
```

This makes the application easier to configure when the SQL Server instance changes.

---

## Recognition Parameters

The LBPH recognizer can be configured with parameters such as:

```text
Radius
Neighbors
Grid X
Grid Y
Threshold
```

A common starting configuration is:

```text
Radius: 1
Neighbors: 8
Threshold: 80
```

These values should be treated as starting points rather than universal values.

Recognition performance depends on:

* Camera quality
* Lighting conditions
* Number of training images
* Face angle
* Facial expression
* Image preprocessing
* Dataset quality

---

## Security Considerations

This project is designed as an educational biometric authentication system.

For a production-grade authentication system, additional security measures should be considered:

* Password hashing using a modern password hashing algorithm
* Encryption for biometric data at rest
* Secure database credentials
* Rate limiting for authentication attempts
* Liveness detection
* Anti-spoofing mechanisms
* Improved biometric recognition models
* Audit logging

LBPH is a classical computer vision algorithm and is useful for learning and experimentation, but it should not be considered sufficient on its own for high-security biometric systems.

---

## Future Improvements

Potential improvements include:

* Deep learning-based face recognition
* Face embedding models
* Liveness detection
* Anti-spoofing detection
* Camera quality validation
* Multi-user recognition
* Encrypted biometric templates
* Role-based authentication
* Authentication logs
* REST API integration
* Centralized database configuration
* Improved application architecture using MVVM
* Unit testing
* Docker-based database setup

---

## License

This project is licensed under the MIT License.

The project also uses OpenCV resources and third-party libraries distributed under their respective licenses.

---

## Authors

**ENZO** *Abod*



GitHub: **[@enzo963](https://github.com/enzo963)**

---

## Disclaimer

BioAuth System was developed as an educational and experimental project for exploring biometric authentication, computer vision, and desktop application development.
