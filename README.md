# Co-Located VR with Hybrid SLAM-based HMD Tracking and Motion Capture Synchronization

This repository provides a system for synchronizing Virtual Reality (VR) head-mounted displays (HMDs) using hybrid SLAM-based tracking combined with external motion capture systems. The system supports multiple VR users and synchronizes their movements in real-time within a shared virtual environment.

## Features

* Compatible with any motion capture system (tested with HTC Vive Tracker and Qualisys).
* Hybrid SLAM-based tracking for accurate VR synchronization.
* Easy configuration for multiple simultaneous VR users.

## Repository Structure

This project contains two Unity projects:

* **Server-side project:** `VR&MotionTracking`
* **Client-side project:** `RealSyncVR`

## Server-side Setup

1. Open the `VR&MotionTracking` Unity project.
2. Open the scene located at `Scenes/Start`.
3. In the scene hierarchy, find the `MotionTrackingData` GameObject:

   * This object contains default player objects (5 players by default).
   * Each player object contains child GameObjects for tracking the head and hands.
4. For each player:

   * Duplicate a player object if you need additional players.
   * Ensure the `Head` GameObject receives correct motion capture data.
   * Disable any unused GameObjects (e.g., unused hand objects).
   * Assign a unique ID in the `SyncTransform` script component on each tracking object.
   * **Important:** Note this unique ID as it will be required for the client-side setup.
5. If using Qualisys:

   * Ensure the RT Object component is attached for data streaming.
   * For other systems, remove the RT Object component.
6. Press the **Play** button in Unity to start the server.

## Client-side Setup (Meta Quest 2/3)

1. Open the `RealSyncVR` Unity project.
2. Load the scene located at `01_GameDemo`.
3. Customize the environment in the scene as desired.
4. For each player instance:

   * In the hierarchy, navigate to the `MotionTrackingData` GameObject.
   * Verify that each player object's `SyncTransform` ID matches the corresponding server-side player ID.
   * Ensure all player GameObjects are enabled and correctly configured.
5. Set the current player's number:

   * Locate the `GameManager` GameObject.
   * Update the `Player Number` field to match the player you are setting up (e.g., Player 1, Player 2, etc.).
6. Build and deploy to Meta Quest:

   * Go to `File > Build Settings`.
   * Select `Android` as the build platform.
   * Adjust build quality settings as desired.
   * Connect your Meta Quest 2/3 device and build directly onto it.
   * **Note:** Each player requires a separate build with their respective player number set in the `GameManager`.

## Adding Additional Tracked Objects

To synchronize additional physical objects:

* Add an empty GameObject to the server-side scene and assign it a unique ID via a `SyncTransform` component.
* On the client side, add the corresponding digital representation and assign it the same unique ID.

## Requirements

* Unity (compatible version for your Meta Quest device)
* Motion capture system (e.g., Qualisys, HTC Vive Tracker)
* Meta Quest 2/3 devices

## Tested Configurations

* HTC Vive Tracker
* Qualisys Motion Capture System
