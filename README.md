# Secure Image Upload Microservices

This project demonstrates a secure image upload workflow using two .NET 9 microservices, `AppService` and `StorageService`.

## Architecture

```
+--------+      +-----------------+      +-----------------+
| Seller |----->|   AppService    |----->| StorageService  |
+--------+      +-----------------+      +-----------------+
    |             1. Request Upload URL          |
    |------------------------------------------->| 2. Upload Image
    |             3. Create Product              |
    |                                            |
```

## Workflow

1.  **Request Upload URL**: The seller sends a request to `AppService` with image metadata.
    `AppService` signs the metadata and asks `StorageService` for a pre-signed URL.
2.  **Upload Image**: The seller uses the pre-signed URL to upload the image directly to `StorageService`.
3.  **Create Product**: The seller sends a request to `AppService` with the product information and the `ImageID` received from `StorageService`.

## How to Run

1.  Make sure you have Docker and Docker Compose installed.
2.  Run the following command from the root of the project:

    ```bash
    docker-compose up --build
    ```

## API Usage

To use the API endpoints via Swagger UI (available at `http://localhost:7071/swagger` for AppService and `http://localhost:7072/swagger` for StorageService):

1.  Open the Swagger UI in your browser.
2.  Click on the "Authorize" button.
3.  In the dialog, enter `SuperSecretApiKey` as the value for `X-Api-Key`.
4.  Click "Authorize" and then "Close".

Now you can try out the endpoints. Alternatively, you can use `curl`:

### 1. Request Upload URL

```bash
curl -X POST \
  http://localhost:7071/products/request-upload-url \
  -H 'Content-Type: application/json' \
  -H 'X-Api-Key: SuperSecretApiKey' \
  -d '{
    "fileName": "my-image.jpg",
    "fileSize": 12345,
    "contentType": "image/jpeg"
  }'
```

### 2. Upload Image

Use the `uploadUrl` from the previous response to upload the image.

```bash
curl -X POST \
  'PASTE_UPLOAD_URL_HERE' \
  --data-binary '@path/to/your/image.jpg'
```

### 3. Create Product

```bash
curl -X POST \
  http://localhost:7071/products \
  -H 'Content-Type: application/json' \
  -H 'X-Api-Key: SuperSecretApiKey' \
  -d '{
    "productName": "My Awesome Product",
    "imageId": "PASTE_IMAGE_ID_HERE"
  }'
```
