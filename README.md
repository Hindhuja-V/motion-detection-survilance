# Motion Detection Surveillance

Simple UWP app to detect motion based on Last frame comparison. Supports remote access, web-push and email alerts.

## Major Technologies Used

- UWP camera api, SoftwareBitmap
- Base64 Encoding
- Socket Listener
- Service Worker

## Screenshots

> Dashboard
![Web Portal](https://ankur198.github.io/MotionDetectionSurvilance/Photos/image(1).png)

> Web Push
![Web Portal](https://ankur198.github.io/MotionDetectionSurvilance/Photos/image(2).png)

> Email Settings
![Web Portal](https://ankur198.github.io/MotionDetectionSurvilance/Photos/image(3).png)

> Web Portal
![Web Portal](https://ankur198.github.io/MotionDetectionSurvilance/Photos/image.png)


## Want to contribute?

This is a personal project and I don't have any template(yet) for PR. Try to keep it event driven as the project currently is.

Any refactoring(especially in XAML) or feature are welcome 😁

## System Requirements

- Win 10 1809 and above
- Camera
- x86/x64/ARM arch

# Setup

### **Pre Compiled** (x86/x64 build only)

- Download latest release from [release](https://github.com/ankur198/MotionDetectionSurvilance/releases) (x86/x64 build only)

### **Compiling on own**

Create Resource file (.resw) and name it ApiKey.resw in /MotionDetectionSurvilance directory.

Add following key to it

- AppCenter(apikey) for ms app center
- SendGrid(apikey) for email 
- FromEmail(email) for sending email