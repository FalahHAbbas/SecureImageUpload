#!/bin/bash

# Ensure jq is installed
if ! command -v jq &> /dev/null
then
    echo "jq could not be found. Please install it (e.g., sudo apt-get install jq or brew install jq)."
    exit 1
fi

APP_SERVICE_URL="http://localhost:7071"
API_KEY="SuperSecretApiKey"
UPLOAD_DIR="./test_uploads"

# Create upload directory if it doesn't exist
mkdir -p "$UPLOAD_DIR"

# Function to generate a dummy image file
generate_image() {
    local filename="$1"
    local size_kb="$2"
    local file_type="$3" # e.g., jpg, png

    local output_file="$UPLOAD_DIR/$filename"
    local mime_type=""

    # Create a dummy file with random content
    head /dev/urandom | tr -dc A-Za-z0-9_ | head -c "${size_kb}K" > "$output_file"

    # Append a simple header/footer to make it a valid (though empty) image
    if [[ "$file_type" == "jpg" ]]; then
        printf '\xFF\xD8\xFF\xE0\x00\x10JFIF\x00\x01\x01\x00\x00\x01\x00\x01\x00\x00\xFF\xD9' >> "$output_file"
        mime_type="image/jpeg"
    elif [[ "$file_type" == "png" ]]; then
        printf '\x89PNG\r\n\x1a\n\x00\x00\x00\rIHDR\x00\x00\x00\x01\x00\x00\x00\x01\x08\x06\x00\x00\x00\x1f\x15\xc4\x89\x00\x00\x00\x0cIDATx\xda\xed\xc1\x01\x01\x00\x00\x00\xc2\xa0\xf7Om\x00\x00\x00\x00IEND\xaeB`\x82' >> "$output_file"
        mime_type="image/png"
    else
        echo "Unsupported file type: $file_type"
        return 1
    fi

    echo "$output_file" "$mime_type"
}

# Test cases: filename_prefix, size_kb, file_type
test_cases=(
    "test_image_small 10 jpg"
    "test_image_medium 50 png"
    "test_image_large 100 jpg"
)

for test_case in "${test_cases[@]}"; do
    read -r filename_prefix size_kb file_type <<< "$test_case"

    echo "--- Testing with ${filename_prefix}.${file_type} (Size: ${size_kb}KB) ---"

    # Step 1: Generate Image
    echo "1. Generating dummy image..."
    read -r image_path mime_type <<< "$(generate_image "${filename_prefix}.${file_type}" "$size_kb" "$file_type")"
    if [[ ! -f "$image_path" ]]; then
        echo "Error: Failed to generate image at $image_path"
        continue
    fi
    file_size=$(stat -c%s "$image_path")
    echo "2. Requesting upload URL from AppService..."
    REQUEST_UPLOAD_URL_PAYLOAD='{
        "fileName": "'"${filename_prefix}.${file_type}"'",
        "fileSize": '"${file_size}"',
        "contentType": "'"${mime_type}"'"
    }'
    
    response=$(curl -s -X POST "${APP_SERVICE_URL}/products/request-upload-url" \
      -H 'Content-Type: application/json' \
      -H "X-Api-Key: ${API_KEY}" \
      -d "${REQUEST_UPLOAD_URL_PAYLOAD}")

    upload_url=$(echo "$response" | jq -r '.uploadUrl')

    if [[ -z "$upload_url" || "$upload_url" == "null" ]]; then
        echo "Error: Failed to get upload URL. Response: $response"
        continue
    fi
    echo "   Received upload URL: $upload_url"

    # Step 3: Upload Image
    echo "3. Uploading image to StorageService via pre-signed URL..."
    upload_response=$(curl -s -X POST "$upload_url"       --data-binary "@$image_path")    image_id=$(echo "$upload_response" | jq -r '.imageId' | tr -d '\n')

    if [[ -z "$image_id" || "$image_id" == "null" ]]; then
        echo "Error: Failed to upload image or get ImageId. Response: $upload_response"
        continue
    fi
    echo "   Image uploaded. Image ID: $image_id"

    # Step 4: Create Product
    echo "4. Creating product in AppService..."
    CREATE_PRODUCT_PAYLOAD='{
        "productName": "Product for '"${filename_prefix}.${file_type}"'",
        "imageId": "'"${image_id}"'"
    }'

    product_response=$(curl -s -X POST "${APP_SERVICE_URL}/products"       -H 'Content-Type: application/json'       -H "X-Api-Key: ${API_KEY}"       -d "${CREATE_PRODUCT_PAYLOAD}")        product_id=$(echo "$product_response" | jq -r '.productId' | tr -d '\n')

    if [[ -z "$product_id" || "$product_id" == "null" ]]; then
        echo "Error: Failed to create product. Response: $product_response"
        continue
    fi
    echo "   Product created. Product ID: $product_id"

    echo "--- Test for ${filename_prefix}.${file_type} completed successfully ---"
    echo ""
done

echo "Cleaning up generated images..."
rm -rf "$UPLOAD_DIR"
echo "Test workflow completed."
