# RedisMasteryDemo

A professional sample project demonstrating full mastery of Redis in ASP.NET Core.

## Day 1: Introduction and Setup

* Introduction to Redis and its use cases
* Setup using Docker
* Initial connection using StackExchange.Redis
* Health Check Endpoint

### How to Run

1. `docker run --name redis-mastery -d -p 6379:6379 redis:latest`
2. Run the project in Visual Studio
3. Navigate to `/redis-health`

## Day 2: Basic Redis Data Type Operations

* **String**: Storing and retrieving simple values
* **List**: Queue and list operations
* **Set**: Unique collections
* **Hash**: Structured data storage
* **Sorted Set**: Ranking and leaderboard

### Implemented Endpoints:

* String Operations
* List Operations
* Set Operations
* Hash Operations
* Sorted Set (Leaderboard)

## Day 3: Basic Caching and Cache-Aside Pattern

* Repository Pattern implementation
* Cache-Aside Pattern with Redis
* Cached Repository
* Cache invalidation on data changes
