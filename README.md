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

## Day 4: Distributed Cache with `IDistributedCache`

* Configuring `AddStackExchangeRedisCache`
* Implementing `DistributedCacheService`
* Supporting JSON serialization
* Absolute expiration
* Sliding expiration
* Best practices for cache management

## Day 5: Session Management with Redis

* Configuring Distributed Session
* Storing and retrieving Session data in Redis
* Managing Session timeout
* Cookie security
* Custom Session Store (ready for extension)

## Day 6: Rate Limiting with Redis (Token Bucket)

* Implementing the Token Bucket algorithm
* Building a Rate Limiter with Lua Script (Atomic operations)
* Custom middleware
* Protecting APIs against abuse

## Day 7: Pub/Sub and Messaging with Redis

* Implementing Redis Publish/Subscribe
* Sending and receiving real-time messages
* Managing channels
* Professional `RedisPubSubService`
* Use cases in notifications and chat systems

## Day 8: Transactions, Pipelining, and Batch Operations

* Redis Transactions (Atomic Operations)
* Pipelining for reducing network latency
* Batch Operations
* Performance comparison benchmark
* Optimization best practices
