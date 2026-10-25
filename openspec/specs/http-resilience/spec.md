# HTTP sessions and resilience

## Purpose

Bound service requests, preserve isolated authentication and coordinate retries and rate limits across sessions.

## Requirements

### Requirement: Isolated sessions and safe redirects

Each HTTP session SHALL own a CookieContainer and HttpClientHandler. Adapter-resolved addresses and redirects SHALL stay on the configured service host over HTTPS. Login redirects using 301 or 302 SHALL change POST to GET.

#### Scenario: Cross-host redirect

- **WHEN** a service redirects a login POST to another host
- **THEN** the transport rejects the redirect before replaying credentials

#### Scenario: Cookie preservation

- **WHEN** responses set session cookies before and during a redirect
- **THEN** the same session sends those cookies in later requests

### Requirement: Bounded retry policy

Safe requests SHALL retry HTTP 408, 429, 500, 502, 503 and 504, recognized HTML rate-limit responses, network failures and per-attempt timeouts. The default retry count SHALL be four, with exponential delay and jitter. Login and upload POSTs SHALL NOT opt into automatic replay.

#### Scenario: HTML overload

- **WHEN** HTTP 200 contains Too many queries
- **THEN** the request observes the shared cooldown and stops after the configured retry count

#### Scenario: Retry-After date

- **WHEN** the response includes a future Retry-After date or a delta
- **THEN** the retry does not run before that server delay

### Requirement: Timeout and cancellation budgets

The default attempt timeout SHALL be 60 seconds and the logical request budget SHALL be five minutes. Caller cancellation SHALL interrupt scheduler waits and HTTP operations. A job-wide deadline SHALL be controlled by the caller token.

#### Scenario: Cancellation during cooldown

- **WHEN** a caller cancels while waiting for the service
- **THEN** waiting ends with caller cancellation rather than a timeout result

### Requirement: eQSL generation pacing

The shared scheduler SHALL start eQSL generation spacing at one second and increase it by 500 milliseconds for each recognized eQSL rate-limit response. Learned spacing SHALL be shared across sessions and retries for the scheduler lifetime. DI SHALL require an initial interval of at least one second. General requests SHALL default to a 500 millisecond minimum interval.

#### Scenario: Shared generation interval

- **WHEN** two sessions request eQSL card generation
- **THEN** the second request waits for the shared generation reservation

#### Scenario: Repeated rate limits

- **WHEN** eQSL responds with HTTP 429 or a recognized rate-limit message
- **THEN** generation spacing increases from 1 to 1.5 seconds, then 2 seconds for the next such response, extending existing reservations
- **AND** successful responses do not reset the learned interval; other transient errors do not increase it
- **AND** server Retry-After and the shared retry cooldown still apply and are never shortened by generation pacing

#### Scenario: Scheduler lifetime

- **WHEN** another download uses the same service provider
- **THEN** it retains the learned generation spacing
- **WHEN** the application creates a new provider and scheduler
- **THEN** generation spacing starts again at the configured initial interval

### Requirement: Sanitized error context

HTTP error messages SHALL identify the method and final endpoint path without query parameters or response bodies.

#### Scenario: Error after redirect

- **WHEN** a redirect destination returns 404 and its query contains a password
- **THEN** the error includes the final path but no query values

## Implementation references

- [SQ7MRU.Utils/Infrastructure/HttpTransport.cs](../../../SQ7MRU.Utils/Infrastructure/HttpTransport.cs)
- [SQ7MRU.Utils/ServiceCollectionExtensions.cs](../../../SQ7MRU.Utils/ServiceCollectionExtensions.cs)

## Verification references

- `TransportTests.HonorsRetryAfterDeltaAndDate`
- `TransportTests.HtmlRateLimitHasBoundedRetries`
- `TransportTests.UnsafePostsAreNeverReplayed`
- `TransportTests.NotFoundIdentifiesFinalEndpointWithoutLeakingQueryParameters`
- `CookieSessionTests.CookieContainerKeepsSeedAndRedirectCookiesForSubsequentRequests`
- `ReviewRegressionTests.CardGenerationIntervalIsSharedAcrossSessionsAndCancellable`
- `ReviewRegressionTests.RepeatedRateLimitsAddHalfASecondAndExtendPendingReservations`
- `TransportTests.EqslLearnsSharedCardSpacingOnlyFromRateLimits`
