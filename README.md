# 🛡️ Cloud Resilience Patterns (.NET 10)

This repository serves as a technical showcase for building high-availability distributed systems using **Polly** in **.NET 10**. 

Drawing from my **13 years of experience at Microsoft** (Azure Synapse, OneLake, and Singularity), these patterns demonstrate how to architect "Secure by Default" and "Self-Healing" service planes that maintain 99.9% availability.

## 🏗️ Architecture: Layered Resilience
This demo implements a **Policy Wrap**, combining two distinct defense layers:

### 1. Retry with Exponential Backoff
- **Strategy:** Retries failed requests 3 times.
- **Backoff Math:** Uses $2^{attempt}$ seconds (2s, 4s, 8s).
- **Purpose:** Handles transient "blips" while preventing "Thundering Herd" scenarios in planet-scale telemetry pipelines.

### 2. State-Based Circuit Breaker
- **Threshold:** Trips after 2 consecutive failures.
- **Cool-down:** 10-second "Open" state to allow downstream recovery.
- **Purpose:** Prevents cascading failures across microservices.

## 📊 Simulation Results
The following console output demonstrates a **Total Service Outage** scenario:
1. **Red/Cyan:** Initial attempts and exponential retries.
2. **Yellow:** Circuit trips to **OPEN** state after threshold is hit.
3. **Gray:** Requests are **Blocked** immediately (Fail-Fast), saving compute cycles.
4. **Magenta:** Circuit enters **HALF-OPEN** to probe for service recovery.

![Resilience Demo Output](your-screenshot-link-here.png)
<img width="444" height="738" alt="AdobeExpressPhotos_635d5f070cef4f289761cf8ba5cc96bf_CopyEdited" src="https://github.com/user-attachments/assets/ddbf6ea2-1362-4358-a1bd-5620bd71fdd4" />


## 🛠️ Tech Stack
- **Language:** C# / .NET 10
- **Library:** [Polly](https://github.com/App-vNext/Polly)
- **Environment:** Visual Studio 2026
