# Code Review Checklist - AutoDocOps

## 🔍 Security & Performance

### Memory Management
- [ ] Use `ArrayPool<byte>` or `MemoryPool<byte>` for large buffer allocations
- [ ] Dispose of `IDisposable` resources properly (using statements)
- [ ] Avoid keeping large objects in memory unnecessarily
- [ ] Check for memory leaks in long-running operations

### Resource Management  
- [ ] All streams, connections, and disposable resources use `using` statements
- [ ] No resource leaks in exception scenarios
- [ ] Proper cancellation token usage for async operations
- [ ] Timeout configurations for external calls

### Input Validation
- [ ] All user inputs are validated and sanitized
- [ ] Request body size limits are enforced
- [ ] API rate limiting is implemented where needed
- [ ] SQL injection protection (parameterized queries)

## 📊 Code Quality

### Magic Numbers & Constants
- [ ] No magic numbers in code - use named constants
- [ ] Constants are grouped in appropriate classes/sections
- [ ] Configuration values come from appsettings/environment variables
- [ ] Timeouts and limits are configurable

### Error Handling
- [ ] Exceptions are logged with appropriate level and context
- [ ] User-facing error messages don't expose internal details
- [ ] Critical operations have proper fallback mechanisms
- [ ] Async methods handle cancellation properly

### Testing
- [ ] Unit tests cover edge cases and error scenarios
- [ ] Integration tests validate end-to-end scenarios
- [ ] Performance tests for critical paths
- [ ] Security tests for authentication/authorization

## 🏗️ Architecture

### Dependency Injection
- [ ] Services are registered with appropriate lifetime
- [ ] No circular dependencies
- [ ] Interfaces are used for testability
- [ ] Configuration objects use Options pattern

### Async/Await
- [ ] ConfigureAwait(false) used in library code
- [ ] No async void methods (except event handlers)
- [ ] Proper exception handling in async methods
- [ ] CancellationToken passed through call chains

## 📈 Performance

### Database
- [ ] Queries use proper indexing strategy
- [ ] No N+1 query problems
- [ ] Connection strings use connection pooling
- [ ] Async methods used for database operations

### Caching
- [ ] Appropriate caching strategies implemented
- [ ] Cache invalidation logic is correct
- [ ] Memory usage of cached items is reasonable
- [ ] Cache keys are collision-resistant

## 🔧 Maintenance

### Logging
- [ ] Appropriate log levels used
- [ ] Structured logging with proper context
- [ ] No sensitive data in logs
- [ ] Performance-critical paths avoid excessive logging

### Documentation
- [ ] Public APIs have XML documentation
- [ ] Complex algorithms have explanatory comments
- [ ] README updated if public interface changes
- [ ] Breaking changes documented in CHANGELOG

## 🚨 Critical Security Checks

### Authentication & Authorization
- [ ] JWT tokens properly validated
- [ ] User permissions checked before operations
- [ ] API keys validated and rate-limited
- [ ] Session management secure

### Data Protection
- [ ] Secrets not hardcoded in source
- [ ] Environment variables used for sensitive config
- [ ] HTTPS enforced for all external communications
- [ ] Input sanitization prevents XSS/injection attacks

---

**Reviewer:** _____________________  
**Date:** _____________________  
**Branch:** _____________________  
**Status:** [ ] Approved [ ] Needs Changes [ ] Rejected