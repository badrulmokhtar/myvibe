#!/bin/sh
set -eu

ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
BUILD="$ROOT/macos/build"
APP="$BUILD/MyVibe.app"
CONTENTS="$APP/Contents"
ICON_SOURCE="$ROOT/assets/myvibe-icon.png"

rm -rf "$BUILD"
mkdir -p "$CONTENTS/MacOS" "$CONTENTS/Resources"
mkdir -p "$BUILD/module-cache"
[ -s "$ICON_SOURCE" ] || { echo "Missing MyVibe icon: $ICON_SOURCE" >&2; exit 1; }
export CLANG_MODULE_CACHE_PATH="$BUILD/module-cache"
export SWIFT_MODULECACHE_PATH="$BUILD/module-cache"

swiftc -O -parse-as-library \
  -module-cache-path "$BUILD/module-cache" \
  -target arm64-apple-macos13.0 \
  -framework AppKit -framework Security -framework CryptoKit \
  "$ROOT/macos/MyVibe.swift" -o "$CONTENTS/MacOS/MyVibe-arm64"
swiftc -O -parse-as-library \
  -module-cache-path "$BUILD/module-cache" \
  -target x86_64-apple-macos13.0 \
  -framework AppKit -framework Security -framework CryptoKit \
  "$ROOT/macos/MyVibe.swift" -o "$CONTENTS/MacOS/MyVibe-x86_64"
lipo -create "$CONTENTS/MacOS/MyVibe-arm64" "$CONTENTS/MacOS/MyVibe-x86_64" -output "$CONTENTS/MacOS/MyVibe"
rm "$CONTENTS/MacOS/MyVibe-arm64" "$CONTENTS/MacOS/MyVibe-x86_64"
cp "$ROOT/macos/catalog-public-key.pem" "$CONTENTS/Resources/catalog-public-key.pem"
cp "$ROOT/macos/catalog-v2-public-key.pem" "$CONTENTS/Resources/catalog-v2-public-key.pem"
cp "$ROOT/catalog.json" "$CONTENTS/Resources/catalog-v1-test.json"
cp "$ROOT/catalog.json.sig" "$CONTENTS/Resources/catalog-v1-test.json.sig"
sips -s format icns "$ICON_SOURCE" --out "$CONTENTS/Resources/MyVibe.icns" >/dev/null

plutil -create xml1 "$CONTENTS/Info.plist"
plutil -insert CFBundleExecutable -string MyVibe "$CONTENTS/Info.plist"
plutil -insert CFBundleIdentifier -string com.badru.myvibe "$CONTENTS/Info.plist"
plutil -insert CFBundleName -string MyVibe "$CONTENTS/Info.plist"
plutil -insert CFBundleDisplayName -string MyVibe "$CONTENTS/Info.plist"
plutil -insert CFBundleIconFile -string MyVibe.icns "$CONTENTS/Info.plist"
plutil -insert CFBundleShortVersionString -string 0.5.0 "$CONTENTS/Info.plist"
plutil -insert CFBundleVersion -string 1 "$CONTENTS/Info.plist"
plutil -insert LSMinimumSystemVersion -string 13.0 "$CONTENTS/Info.plist"
plutil -insert NSHighResolutionCapable -bool true "$CONTENTS/Info.plist"
[ -s "$CONTENTS/Resources/MyVibe.icns" ] || { echo "MyVibe.icns was not built." >&2; exit 1; }
[ "$(plutil -extract CFBundleIconFile raw -o - "$CONTENTS/Info.plist")" = "MyVibe.icns" ] || { echo "CFBundleIconFile is missing." >&2; exit 1; }

codesign --force --sign - --timestamp=none "$APP"
"$CONTENTS/MacOS/MyVibe" --self-test

TEST_ROOT=$(mktemp -d "${TMPDIR:-/tmp}/myvibe-archive-test.XXXXXX")
trap 'rm -rf "$TEST_ROOT"' EXIT HUP INT TERM
mkdir -p "$TEST_ROOT/safe/CSXS" "$TEST_ROOT/unsafe"
printf '<ExtensionManifest />\n' > "$TEST_ROOT/safe/CSXS/manifest.xml"
ln -s /etc/passwd "$TEST_ROOT/unsafe/passwd-link"
(cd "$TEST_ROOT/safe" && /usr/bin/zip -qr "$TEST_ROOT/safe.zip" .)
(cd "$TEST_ROOT/unsafe" && /usr/bin/zip -qry "$TEST_ROOT/unsafe.zip" .)
"$CONTENTS/MacOS/MyVibe" --preflight-archive "$TEST_ROOT/safe.zip"
if "$CONTENTS/MacOS/MyVibe" --preflight-archive "$TEST_ROOT/unsafe.zip" >/dev/null 2>&1; then
  echo "Unsafe symlink archive unexpectedly passed." >&2
  exit 1
fi

file "$CONTENTS/MacOS/MyVibe"
codesign --verify --deep --strict --verbose=2 "$APP"
echo "$APP"
