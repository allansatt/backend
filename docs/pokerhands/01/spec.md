# Feature: Poker Hands Upload, Transcode, and Download

**Revision**: 01  
**Status**: Draft  
**Created**: 2025-02-20

## Summary

Authenticated users can upload Ignition hand history files to S3 via presigned URLs, have them transcoded to another format (e.g. PokerStars) using the [ignition_hands_converter](https://github.com/allansatt/ignition_hands_converter) tool, and access a downloads experience where they can list and download their transcoded files.

## User stories

- **As a** signed-in user, **I want** to receive a presigned URL so that **I can** upload my hand history file directly to storage without sending the file through the API.
- **As a** signed-in user, **I want** my uploaded file to be transcoded automatically so that **I can** use it in my preferred hand history format (e.g. PokerStars).
- **As a** signed-in user, **I want** a downloads page that lists my transcoded hand histories so that **I can** find and download them.
- **As a** signed-in user, **I want** to download a transcoded file via a secure, time-limited link so that **I can** retrieve it without exposing storage publicly.

## Acceptance criteria

- [ ] Authenticated requests to an "upload URL" endpoint return a presigned URL for the S3 bucket "pokerhands" with a key scoped to the requesting user.
- [ ] Client can upload a file to the returned presigned URL; the file is stored under a defined prefix for that user (e.g. uploads).
- [ ] After a file is uploaded, a transcoding process runs that uses the ignition_hands_converter Python logic to produce a transcoded output.
- [ ] Transcoded output is stored in S3 under a user-scoped prefix (e.g. transcoded) and is associated with the original upload (traceable for listing).
- [ ] An authenticated "list my files" (or equivalent) capability returns the user's transcoded hand histories (and optionally upload metadata) so a downloads page can be populated.
- [ ] Authenticated requests can obtain a presigned download URL for a specific transcoded file the user owns; the URL is time-limited and does not expose long-lived credentials.
- [ ] Unauthenticated or invalid-token requests receive an appropriate error and do not receive presigned URLs or file listings.
- [ ] Only the owning user can list or download their own files; no cross-user access.

## High-level design

- **API surface**: Add API resources under the existing base API (e.g. `/pokerhands` or `/hand-history`) with Cognito authorizer so only authenticated users can call them. Endpoints: (1) request presigned upload URL (POST or GET), (2) list user’s transcoded files (GET), (3) request presigned download URL for a given file (GET).
- **Storage**: One S3 bucket named "pokerhands". Use prefix-based layout per user, e.g. `users/{userId}/uploads/{requestId}/{originalName}` and `users/{userId}/transcoded/{requestId}/{outputName}`. This supports listing by prefix and keeps ownership clear. Optionally a separate "trigger" or metadata store (e.g. DynamoDB) can be used to track upload → transcode job status and metadata if needed; see constraints.
- **Upload flow**: Client calls API with auth; API validates Cognito JWT, generates a unique key under `users/{userId}/uploads/...`, returns presigned PUT URL (and possibly the key/requestId for the client to poll or for webhook). Client uploads file to the presigned URL. Optionally, upload completion is signaled (e.g. S3 event or client callback) to start transcoding.
- **Transcoding**: A backend process (Lambda with Python runtime, or container/Step Functions) is triggered when a new object appears in the uploads prefix (or when API records an upload). It retrieves the object from S3, runs the ignition_hands_converter logic (invoked as library or subprocess), writes the transcoded output to the user’s transcoded prefix, and optionally updates job status if a metadata store exists.
- **Downloads experience**: Frontend (or API) calls "list my files" which lists objects under `users/{userId}/transcoded/` (and optionally correlates with uploads). For each file (or a selected one), the client calls the API to get a presigned GET URL and then uses that URL to download the file. The downloads page is the UI that shows the list and exposes these download actions.
- **Auth**: Reuse existing Cognito User Pool; API Gateway authorizer validates JWT and passes `userId` (sub) into the integration so all keys and listings are scoped by identity.

## Constraints and assumptions

- Assumes existing Cognito-based auth; no new identity provider or SSO in scope.
- Assumes the ignition_hands_converter is available as a callable Python component (library or CLI) and can be run in the same account/region (e.g. Lambda layer, container, or bundled in Lambda).
- S3 bucket "pokerhands" is created and lifecycle/retention are defined elsewhere or in the same stack; encryption and block public access are assumed.
- **File listing strategy**: Listing by S3 prefix (`ListObjectsV2` with prefix `users/{userId}/transcoded/`) is sufficient for a first version and is performant for hundreds to low thousands of objects per user. If the product later needs rich metadata, search, pagination beyond S3’s 1000-object limit, or very high scale per user, a DynamoDB (or similar) table keyed by `userId` and job/file id should be introduced to store metadata and drive the list API; the spec does not mandate DynamoDB for v1.

## Out of scope

- Support for unauthenticated upload or download.
- Transcoding formats other than those supported by ignition_hands_converter (e.g. only Ignition → PokerStars or whatever the tool supports).
- Real-time progress UI for transcoding (polling or webhooks may be in scope; real-time push is out of scope unless explicitly added later).
- Deleting or overwriting existing uploads/transcoded files (no delete/update API in this feature).
- Public or shareable links to transcoded files; downloads are strictly for the owning user via presigned URLs.
- Changes to the ignition_hands_converter tool itself; it is consumed as-is.
