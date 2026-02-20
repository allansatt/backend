# Feature: Poker Hands Upload, Transcode, and Download

**Revision**: 02  
**Status**: Draft  
**Created**: 2025-02-20

## Summary

Authenticated users can upload Ignition hand history files to the S3 bucket "pokerhands" via presigned URLs, have them transcoded to another format (e.g. PokerStars) using the [ignition_hands_converter](https://github.com/allansatt/ignition_hands_converter) Python tool, and use a downloads page to list and download their transcoded files. All new infrastructure for this feature is provisioned with Terraform.

## User stories

- **As a** signed-in user, **I want** to receive a presigned URL for the "pokerhands" bucket so that **I can** upload my hand history file directly to storage without sending the file through the API.
- **As a** signed-in user, **I want** my uploaded file to be transcoded automatically using the ignition_hands_converter tool so that **I can** use it in my preferred hand history format (e.g. PokerStars).
- **As a** signed-in user, **I want** a downloads page that lists my transcoded hand histories so that **I can** find and download them.
- **As a** signed-in user, **I want** to download a transcoded file via a secure, time-limited link so that **I can** retrieve it without exposing storage publicly.

## Acceptance criteria

- [ ] Authenticated requests to an "upload URL" endpoint return a presigned URL for the S3 bucket "pokerhands" with a key scoped to the requesting user.
- [ ] Client can upload a file to the returned presigned URL; the file is stored under a defined prefix for that user (e.g. uploads).
- [ ] After a file is uploaded, a transcoding process runs that uses the ignition_hands_converter Python logic to produce a transcoded output.
- [ ] Transcoded output is stored in S3 under a user-scoped prefix (e.g. transcoded) and is traceable to the original upload for listing.
- [ ] An authenticated "list my files" (or equivalent) capability returns the user's transcoded hand histories so the downloads page can be populated.
- [ ] Authenticated requests can obtain a presigned download URL for a specific transcoded file the user owns; the URL is time-limited and does not expose long-lived credentials.
- [ ] Unauthenticated or invalid-token requests receive an appropriate error and do not receive presigned URLs or file listings.
- [ ] Only the owning user can list or download their own files; no cross-user access.
- [ ] All new infrastructure for this feature (S3 bucket, Lambdas, IAM, event wiring, etc.) is defined and applied via Terraform.

## High-level design

- **API surface**: Expose endpoints (e.g. under `/pokerhands` or `/hand-history`) protected by the existing Cognito authorizer: (1) request presigned upload URL (POST or GET), (2) list user's transcoded files (GET), (3) request presigned download URL for a given file (GET). These may be implemented via API Gateway plus Lambda or existing API stack; only net-new resources (e.g. Lambdas, S3, IAM for this feature) are provisioned in Terraform.
- **Storage**: One S3 bucket named "pokerhands". Prefix-based layout per user: e.g. `users/{userId}/uploads/{requestId}/{originalName}` and `users/{userId}/transcoded/{requestId}/{outputName}`. This supports listing by prefix and keeps ownership clear. **File listing**: Use S3 prefix listing (`ListObjectsV2` with prefix `users/{userId}/transcoded/`) for v1; no DynamoDB required unless scale or product needs justify it (see Constraints and assumptions).
- **Upload flow**: Client calls API with auth; API validates Cognito JWT, generates a unique key under `users/{userId}/uploads/...`, returns presigned PUT URL (and optionally the key/requestId). Client uploads file to the presigned URL. Upload completion triggers transcoding (e.g. S3 event notification to Lambda).
- **Transcoding**: A process (e.g. Lambda with Python runtime) is triggered when a new object appears in the uploads prefix. It retrieves the object from S3, runs the ignition_hands_converter logic (as library or subprocess), writes the transcoded output to the user's transcoded prefix.
- **Downloads experience**: Frontend calls "list my files" (backed by S3 list by prefix); for each file the client requests a presigned GET URL from the API and uses it to download. The downloads page is the UI that shows the list and exposes download actions.
- **Auth**: Reuse existing Cognito User Pool; authorizer validates JWT and passes `userId` (sub) so all keys and listings are scoped by identity.
- **Infrastructure**: Terraform modules (or root modules) define the "pokerhands" S3 bucket, bucket policies, any Lambda functions and their IAM roles, S3 event notifications, and integration points (e.g. API Gateway routes/Lambda permissions) for this feature. Existing CDK stacks are not extended for this feature; net-new resources are Terraform-managed.

## Constraints and assumptions

- Assumes existing Cognito-based auth; no new identity provider or SSO in scope.
- Assumes ignition_hands_converter is available as a callable Python component (library or CLI) and can be run in the same account/region (e.g. Lambda layer, container, or bundled in Lambda).
- S3 bucket "pokerhands" is created in Terraform with encryption and block public access; lifecycle/retention can be defined in Terraform or later.
- **DynamoDB vs S3 prefix listing**: Using S3 prefixes for listing is sufficient for v1 and is performant for hundreds to low thousands of objects per user. A separate DynamoDB table for file/job metadata is **not** required for the initial release. Introduce DynamoDB (or similar) later if the product needs rich metadata, search, pagination beyond S3’s 1000-object limit per request, or very high scale per user.

## Out of scope

- Support for unauthenticated upload or download.
- Transcoding formats other than those supported by ignition_hands_converter.
- Real-time progress UI for transcoding (polling or webhooks may be in scope; real-time push is out of scope unless added later).
- Deleting or overwriting existing uploads/transcoded files (no delete/update API in this feature).
- Public or shareable links to transcoded files; downloads are strictly for the owning user via presigned URLs.
- Changes to the ignition_hands_converter tool itself; it is consumed as-is.
- Provisioning this feature’s new resources via CDK; Terraform only for this feature.
