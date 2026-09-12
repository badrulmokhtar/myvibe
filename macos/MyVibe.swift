import AppKit
import CryptoKit
import Foundation
import Security

private let managerVersion = "0.5.0"
private let trustedReleasePrefix = "/badrulmokhtar/myvibe/releases/download/"
private let maxArchiveBytes: Int64 = 200 * 1024 * 1024
private let maxExtractedBytes: Int64 = 600 * 1024 * 1024
private let maxEntries = 4096
private let maxCatalogBytes = 1_048_576
private let maxCatalogSignatureBytes = 16_384

struct RegistryProduct {
    let id: String
    let name: String
    let version: String
    let description: String
    let nativeName: String?
    let cepName: String
}

enum Registry {
    static let products = [
        RegistryProduct(id: "com.badru.transform2d5", name: "2.5D Transform", version: "0.9.6", description: "Non-destructive 2.5D transforms for Adobe Illustrator.", nativeName: "2.5D Transform.aip", cepName: "2.5D Transform"),
        RegistryProduct(id: "com.badru.tonemesh", name: "ToneMesh", version: "0.9.9", description: "Editable halftone fields with custom marks and low-overhead hidden-panel monitoring.", nativeName: "ToneMesh.aip", cepName: "ToneMesh"),
        RegistryProduct(id: "com.badru.logolize", name: "Logolize", version: "1.6.4", description: "Generate responsive, editable logo systems in Illustrator.", nativeName: nil, cepName: "Logolize")
    ]

    static func product(id: String) -> RegistryProduct? { products.first { $0.id == id } }
}

struct SignaturePolicy: Codable, Equatable {
    let type: String
    let required: Bool
    let teamIdentifier: String?
}

struct Artifact: Codable, Equatable {
    let platform: String
    let architecture: String
    let downloadUrl: String
    let sha256: String
    let signature: SignaturePolicy
}

struct Product: Codable, Identifiable {
    let id: String
    let name: String
    let version: String
    let artifacts: [Artifact]
    let description: String?
    let host: String?
    let hostVersion: String?
    let minimumManagerVersion: String?
    let releases: [ProductRelease]?

    func selecting(_ release: ProductRelease) -> Product {
        Product(id: id, name: name, version: release.version, artifacts: release.artifacts, description: description,
                host: host, hostVersion: hostVersion, minimumManagerVersion: minimumManagerVersion, releases: releases)
    }
}

struct ProductRelease: Codable {
    let version: String
    let releasedAt: String
    let artifacts: [Artifact]
}

struct Catalog: Codable {
    let schemaVersion: Int
    let channel: String
    let manager: Product
    let plugins: [Product]
}

enum MyVibeError: LocalizedError {
    case invalid(String)
    var errorDescription: String? {
        if case let .invalid(message) = self { return message }
        return "MyVibe failed."
    }
}

enum Safety {
    static func trustedReleaseURL(_ value: String) -> Bool {
        guard let url = URL(string: value), url.scheme == "https", url.host?.lowercased() == "github.com",
              url.user == nil, url.password == nil else { return false }
        return url.path.lowercased().hasPrefix(trustedReleasePrefix)
    }

    static func artifact(for product: Product) throws -> Artifact {
        let arch = ProcessInfo.processInfo.machineArchitecture
        guard let artifact = product.artifacts.first(where: {
            $0.platform == "macos" && ($0.architecture == "universal" || $0.architecture == arch)
        }) else { throw MyVibeError.invalid("\(product.name) has no compatible macOS artifact.") }
        guard trustedReleaseURL(artifact.downloadUrl), artifact.sha256.range(of: "^[A-Fa-f0-9]{64}$", options: .regularExpression) != nil,
              ["developer-id", "adobe-cep"].contains(artifact.signature.type) else {
            throw MyVibeError.invalid("\(product.name) has unsafe catalog metadata.")
        }
        if artifact.signature.required && artifact.signature.type == "developer-id" && artifact.signature.teamIdentifier?.range(of: "^[A-Z0-9]{10}$", options: .regularExpression) == nil {
            throw MyVibeError.invalid("\(product.name) is missing its required Developer ID team.")
        }
        return artifact
    }

    static func verifyCatalog(_ data: Data, signatureData: Data, publicKeyPEM: String) throws -> Catalog {
        guard data.count <= maxCatalogBytes, signatureData.count <= maxCatalogSignatureBytes,
              verifySignature(data, signatureData: signatureData, publicKeyPEM: publicKeyPEM) else {
            throw MyVibeError.invalid("The catalog signature is invalid.")
        }
        let catalog = try JSONDecoder().decode(Catalog.self, from: data)
        guard catalog.schemaVersion == 2, ["beta", "stable"].contains(catalog.channel), catalog.manager.id == "com.badru.myvibe", !catalog.plugins.isEmpty else {
            throw MyVibeError.invalid("The catalog structure is invalid.")
        }
        var identifiers = Set<String>()
        for product in [catalog.manager] + catalog.plugins {
            guard identifiers.insert(product.id).inserted,
                  product.version.range(of: "^\\d+\\.\\d+\\.\\d+$", options: .regularExpression) != nil else {
                throw MyVibeError.invalid("\(product.name) has an invalid version.")
            }
            let selectedArtifact = try artifact(for: product)
            if catalog.channel == "stable" && !selectedArtifact.signature.required {
                throw MyVibeError.invalid("Stable artifacts must require a trusted signature.")
            }
            if let releases = product.releases {
                guard releases.first?.version == product.version,
                      releases.first?.artifacts == product.artifacts,
                      Set(releases.map(\.version)).count == releases.count,
                      zip(releases, releases.dropFirst()).allSatisfy({ pair in pair.0.version.compare(pair.1.version, options: .numeric) == .orderedDescending }) else {
                    throw MyVibeError.invalid("\(product.name) has invalid release history.")
                }
                for release in releases {
                    guard release.version.range(of: "^\\d+\\.\\d+\\.\\d+$", options: .regularExpression) != nil,
                          release.releasedAt.range(of: "^\\d{4}-\\d{2}-\\d{2}$", options: .regularExpression) != nil else {
                        throw MyVibeError.invalid("\(product.name) has invalid release metadata.")
                    }
                    let historicalArtifact = try artifact(for: product.selecting(release))
                    if catalog.channel == "stable" && !historicalArtifact.signature.required {
                        throw MyVibeError.invalid("Stable release history must require a trusted signature.")
                    }
                }
            }
        }
        guard catalog.plugins.allSatisfy({
            $0.host == "Adobe Illustrator" && $0.hostVersion == "30.x"
                && $0.minimumManagerVersion?.range(of: "^\\d+\\.\\d+\\.\\d+$", options: .regularExpression) != nil
        }) else {
            throw MyVibeError.invalid("The catalog plug-in compatibility is invalid.")
        }
        return catalog
    }

    static func verifySignature(_ data: Data, signatureData: Data, publicKeyPEM: String) -> Bool {
        guard let signature = Data(base64Encoded: String(decoding: signatureData, as: UTF8.self).trimmingCharacters(in: .whitespacesAndNewlines)),
              let keyData = pemData(publicKeyPEM) else { return false }
        let attributes: [CFString: Any] = [kSecAttrKeyType: kSecAttrKeyTypeRSA, kSecAttrKeyClass: kSecAttrKeyClassPublic]
        guard let key = SecKeyCreateWithData(keyData as CFData, attributes as CFDictionary, nil),
              SecKeyVerifySignature(key, .rsaSignatureMessagePKCS1v15SHA256, data as CFData, signature as CFData, nil) else {
            return false
        }
        return true
    }

    static func verifySHA256(_ file: URL, expected: String) throws {
        let handle = try FileHandle(forReadingFrom: file)
        defer { try? handle.close() }
        var hasher = SHA256()
        while let chunk = try handle.read(upToCount: 1024 * 1024), !chunk.isEmpty { hasher.update(data: chunk) }
        let actual = hasher.finalize().map { String(format: "%02X", $0) }.joined()
        guard actual.caseInsensitiveCompare(expected) == .orderedSame else { throw MyVibeError.invalid("The package SHA-256 does not match the signed catalog.") }
    }

    static func preflightArchive(_ archive: URL) throws -> [String] {
        let size = try archive.resourceValues(forKeys: [.fileSizeKey]).fileSize ?? 0
        guard size > 0, size <= maxArchiveBytes else { throw MyVibeError.invalid("The package is empty or too large.") }
        let output = try run("/usr/bin/unzip", ["-Z1", archive.path])
        let listing = try run("/usr/bin/zipinfo", ["-l", archive.path])
        let totals = try run("/usr/bin/unzip", ["-Zt", archive.path])
        let totalRange = totals.range(of: "[0-9]+ bytes uncompressed", options: .regularExpression)
        let uncompressed = totalRange.flatMap { Int64(totals[$0].split(separator: " ")[0]) }
        guard let uncompressed, uncompressed <= maxExtractedBytes else { throw MyVibeError.invalid("The package expands beyond the safe size limit.") }
        guard !listing.split(whereSeparator: \Character.isNewline).contains(where: { $0.first == "l" }) else {
            throw MyVibeError.invalid("The package contains a symbolic link.")
        }
        let entries = output.split(whereSeparator: \Character.isNewline).map(String.init)
        guard !entries.isEmpty, entries.count <= maxEntries else { throw MyVibeError.invalid("The package contains an unsafe number of files.") }
        for entry in entries {
            let normalized = entry.replacingOccurrences(of: "\\", with: "/")
            guard !normalized.hasPrefix("/"), !normalized.split(separator: "/").contains(".."), !normalized.contains("\0") else {
                throw MyVibeError.invalid("The package contains an unsafe path.")
            }
        }
        return entries
    }

    static func validateExtractedTree(_ root: URL) throws {
        let keys: Set<URLResourceKey> = [.isSymbolicLinkKey, .isRegularFileKey, .fileSizeKey]
        guard let enumerator = FileManager.default.enumerator(at: root, includingPropertiesForKeys: Array(keys)) else {
            throw MyVibeError.invalid("The extracted package cannot be inspected.")
        }
        var count = 0
        var bytes: Int64 = 0
        for case let file as URL in enumerator {
            count += 1
            let values = try file.resourceValues(forKeys: keys)
            guard values.isSymbolicLink != true else { throw MyVibeError.invalid("The package contains a symbolic link.") }
            if values.isRegularFile == true { bytes += Int64(values.fileSize ?? 0) }
            guard count <= maxEntries, bytes <= maxExtractedBytes else { throw MyVibeError.invalid("The extracted package is too large.") }
        }
    }

    private static func pemData(_ pem: String) -> Data? {
        let body = pem.components(separatedBy: .newlines).filter { !$0.hasPrefix("-----") }.joined()
        return Data(base64Encoded: body)
    }
}

enum Installer {
    static let support = FileManager.default.homeDirectoryForCurrentUser.appending(path: "Library/Application Support/MyVibe")
    static let downloads = support.appending(path: "downloads")
    static let backups = support.appending(path: "backups")
    static let cepRoot = FileManager.default.homeDirectoryForCurrentUser.appending(path: "Library/Application Support/Adobe/CEP/extensions")
    static let illustrator = URL(fileURLWithPath: "/Applications/Adobe Illustrator 2026/Adobe Illustrator.app")
    static let pluginRoot = URL(fileURLWithPath: "/Applications/Adobe Illustrator 2026/Plug-ins.localized")

    static func illustratorRunning() -> Bool {
        NSWorkspace.shared.runningApplications.contains { $0.bundleIdentifier == "com.adobe.illustrator" || $0.localizedName == "Adobe Illustrator" }
    }

    static func download(_ artifact: Artifact, progress: @escaping (Double) -> Void) async throws -> URL {
        try FileManager.default.createDirectory(at: downloads, withIntermediateDirectories: true)
        guard let url = URL(string: artifact.downloadUrl) else { throw MyVibeError.invalid("The download URL is invalid.") }
        let target = downloads.appending(path: url.lastPathComponent)
        try? FileManager.default.removeItem(at: target)
        let partial = target.appendingPathExtension("partial")
        try? FileManager.default.removeItem(at: partial)
        do {
            _ = try await Task.detached {
                try run("/usr/bin/curl", ["--fail", "--location", "--silent", "--show-error", "--proto", "=https", "--proto-redir", "=https", "--max-redirs", "5", "--max-filesize", String(maxArchiveBytes), "--output", partial.path, url.absoluteString])
            }.value
            let size = try partial.resourceValues(forKeys: [.fileSizeKey]).fileSize ?? 0
            guard size > 0, size <= maxArchiveBytes else { throw MyVibeError.invalid("The downloaded package is empty or too large.") }
            try FileManager.default.moveItem(at: partial, to: target)
        } catch {
            try? FileManager.default.removeItem(at: partial)
            throw error
        }
        try Safety.verifySHA256(target, expected: artifact.sha256)
        progress(1)
        return target
    }

    static func inspectAndExtract(_ archive: URL) throws -> URL {
        _ = try Safety.preflightArchive(archive)
        let root = FileManager.default.temporaryDirectory.appending(path: "myvibe-\(UUID().uuidString)")
        try FileManager.default.createDirectory(at: root, withIntermediateDirectories: true)
        do {
            _ = try run("/usr/bin/ditto", ["-x", "-k", "--noqtn", archive.path, root.path])
            try Safety.validateExtractedTree(root)
            return root
        } catch {
            try? FileManager.default.removeItem(at: root)
            throw error
        }
    }

    static func payload(in root: URL, product: Product) throws -> (native: URL?, cep: URL) {
        guard let expected = Registry.product(id: product.id) else { throw MyVibeError.invalid("The catalog contains an unsupported plug-in.") }
        let urls = (FileManager.default.enumerator(at: root, includingPropertiesForKeys: nil)?.allObjects as? [URL]) ?? []
        let native = expected.nativeName.flatMap { name in urls.first(where: { $0.lastPathComponent == name }) }
        guard expected.nativeName == nil || native != nil else { throw MyVibeError.invalid("\(product.name) is missing its expected native plug-in.") }
        guard let cep = urls.first(where: { $0.lastPathComponent == expected.cepName && FileManager.default.fileExists(atPath: $0.appending(path: "CSXS/manifest.xml").path) }) else { throw MyVibeError.invalid("\(product.name) is missing its CEP interface.") }
        return (native, cep)
    }

    static func install(product: Product, archive: URL) throws {
        guard FileManager.default.fileExists(atPath: illustrator.path) else { throw MyVibeError.invalid("Adobe Illustrator 2026 was not found.") }
        guard !illustratorRunning() else { throw MyVibeError.invalid("Close Illustrator, then try again.") }
        let root = try inspectAndExtract(archive)
        defer { try? FileManager.default.removeItem(at: root) }
        let source = try payload(in: root, product: product)
        let artifact = try Safety.artifact(for: product)
        if artifact.signature.required && artifact.signature.type == "developer-id" { try verifyDeveloperID(source.native!, expectedTeam: artifact.signature.teamIdentifier!) }
        if artifact.signature.required && artifact.signature.type == "adobe-cep" && !FileManager.default.fileExists(atPath: source.cep.appending(path: "META-INF/signatures.xml").path) { throw MyVibeError.invalid("The CEP signature is missing.") }
        let safeName = product.id.replacingOccurrences(of: "[^A-Za-z0-9._-]", with: "-", options: .regularExpression)
        let backup = backups.appending(path: "\(safeName)-\(Int(Date().timeIntervalSince1970))")
        try FileManager.default.createDirectory(at: backup, withIntermediateDirectories: true)
        let nativeTarget = source.native.map { pluginRoot.appending(path: $0.lastPathComponent) }
        let cepTarget = cepRoot.appending(path: source.cep.lastPathComponent)
        if FileManager.default.fileExists(atPath: cepTarget.path) { try FileManager.default.copyItem(at: cepTarget, to: backup.appending(path: "CEP")) }
        if let nativeTarget, let native = source.native, FileManager.default.fileExists(atPath: nativeTarget.path) {
            try FileManager.default.copyItem(at: nativeTarget, to: backup.appending(path: native.lastPathComponent))
        }
        do {
            try FileManager.default.createDirectory(at: cepRoot, withIntermediateDirectories: true)
            try? FileManager.default.removeItem(at: cepTarget)
            try FileManager.default.copyItem(at: source.cep, to: cepTarget)
            if let nativeTarget, let native = source.native {
                _ = try? run("/usr/bin/xattr", ["-dr", "com.apple.quarantine", native.path])
                _ = try admin([
                    ("/bin/mkdir", ["-p", pluginRoot.path]),
                    ("/bin/rm", ["-rf", nativeTarget.path]),
                    ("/bin/cp", ["-R", native.path, nativeTarget.path])
                ])
            }
            _ = try? run("/usr/bin/xattr", ["-dr", "com.apple.quarantine", cepTarget.path])
            if !artifact.signature.required && !FileManager.default.fileExists(atPath: cepTarget.appending(path: "META-INF/signatures.xml").path) {
                _ = try run("/usr/bin/defaults", ["write", "com.adobe.CSXS.12", "PlayerDebugMode", "1"])
            }
        } catch {
            do {
                if FileManager.default.fileExists(atPath: cepTarget.path) { try FileManager.default.removeItem(at: cepTarget) }
                let oldCEP = backup.appending(path: "CEP")
                if FileManager.default.fileExists(atPath: oldCEP.path) { try FileManager.default.copyItem(at: oldCEP, to: cepTarget) }
                if let nativeTarget, let native = source.native {
                    let oldNative = backup.appending(path: native.lastPathComponent)
                    var commands: [(String, [String])] = [("/bin/rm", ["-rf", nativeTarget.path])]
                    if FileManager.default.fileExists(atPath: oldNative.path) { commands.append(("/bin/cp", ["-R", oldNative.path, nativeTarget.path])) }
                    _ = try admin(commands)
                }
            } catch {
                throw MyVibeError.invalid("Installation failed and rollback also failed. Recovery backup: \(backup.path)")
            }
            throw error
        }
    }

    static func installedParts(for entry: RegistryProduct) -> (native: Bool, cep: Bool) {
        (entry.nativeName.map { FileManager.default.fileExists(atPath: pluginRoot.appending(path: $0).path) } ?? false,
         FileManager.default.fileExists(atPath: cepRoot.appending(path: entry.cepName).path))
    }

    static func installationComplete(_ parts: (native: Bool, cep: Bool), for entry: RegistryProduct) -> Bool {
        parts.cep && (entry.nativeName == nil || parts.native)
    }

    static func installedVersion(for entry: RegistryProduct) -> String? {
        let manifest = cepRoot.appending(path: entry.cepName).appending(path: "CSXS/manifest.xml")
        guard let text = try? String(contentsOf: manifest, encoding: .utf8),
              let marker = text.range(of: "ExtensionBundleVersion=\"") else { return nil }
        return String(text[marker.upperBound...].prefix { $0 != "\"" })
    }

    static func remove(entry: RegistryProduct) throws {
        guard !illustratorRunning() else { throw MyVibeError.invalid("Close Illustrator, then try again.") }
        let nativeTarget = entry.nativeName.map { pluginRoot.appending(path: $0) }
        let cepTarget = cepRoot.appending(path: entry.cepName)
        let parts = installedParts(for: entry)
        guard parts.native || parts.cep else { throw MyVibeError.invalid("\(entry.name) is not installed.") }

        let safeName = entry.id.replacingOccurrences(of: "[^A-Za-z0-9._-]", with: "-", options: .regularExpression)
        let backup = backups.appending(path: "\(safeName)-before-remove-\(Int(Date().timeIntervalSince1970))")
        try FileManager.default.createDirectory(at: backup, withIntermediateDirectories: true)
        let nativeBackup = entry.nativeName.map { backup.appending(path: $0) }
        let cepBackup = backup.appending(path: "CEP")
        if parts.cep { try FileManager.default.copyItem(at: cepTarget, to: cepBackup) }
        if let nativeTarget, let nativeBackup, parts.native { try FileManager.default.copyItem(at: nativeTarget, to: nativeBackup) }

        do {
            if parts.cep { try FileManager.default.removeItem(at: cepTarget) }
            if let nativeTarget, parts.native { _ = try admin([("/bin/rm", ["-rf", nativeTarget.path])]) }
        } catch {
            do {
                if parts.cep && !FileManager.default.fileExists(atPath: cepTarget.path) { try FileManager.default.copyItem(at: cepBackup, to: cepTarget) }
                if let nativeTarget, let nativeBackup, parts.native && !FileManager.default.fileExists(atPath: nativeTarget.path) { _ = try admin([("/bin/cp", ["-R", nativeBackup.path, nativeTarget.path])]) }
            } catch {
                throw MyVibeError.invalid("Removal failed and rollback also failed. Recovery backup: \(backup.path)")
            }
            throw error
        }
    }

    private static func verifyDeveloperID(_ bundle: URL, expectedTeam: String) throws {
        _ = try run("/usr/bin/codesign", ["--verify", "--deep", "--strict", bundle.path])
        let details = try run("/usr/bin/codesign", ["-dv", "--verbose=4", bundle.path])
        guard details.split(whereSeparator: \Character.isNewline).contains("TeamIdentifier=\(expectedTeam)") else {
            throw MyVibeError.invalid("The plug-in Developer ID does not match the signed catalog.")
        }
    }

    private static func admin(_ commands: [(String, [String])]) throws -> String {
        let command = shellCommand(commands)
        let script = "do shell script \(appleQuote(command)) with administrator privileges"
        return try run("/usr/bin/osascript", ["-e", script])
    }
}

enum CatalogLoader {
    private static let catalogURL = "https://raw.githubusercontent.com/badrulmokhtar/myvibe/main/catalog-v2.json"
    private static let signatureURL = "https://raw.githubusercontent.com/badrulmokhtar/myvibe/main/catalog-v2.json.sig"
    private static let cacheDirectory = Installer.support.appending(path: "catalog")
    private static let cachedCatalog = cacheDirectory.appending(path: "catalog-v2.json")
    private static let cachedSignature = cacheDirectory.appending(path: "catalog-v2.json.sig")

    static func cached() throws -> Catalog? {
        guard FileManager.default.fileExists(atPath: cachedCatalog.path),
              FileManager.default.fileExists(atPath: cachedSignature.path) else { return nil }
        return try verified(cachedCatalog, cachedSignature)
    }

    static func fetchAndCache() throws -> Catalog {
        let root = FileManager.default.temporaryDirectory.appending(path: "myvibe-catalog-\(UUID().uuidString)")
        try FileManager.default.createDirectory(at: root, withIntermediateDirectories: true)
        defer { try? FileManager.default.removeItem(at: root) }
        let catalogFile = root.appending(path: "catalog-v2.json")
        let signatureFile = root.appending(path: "catalog-v2.json.sig")
        _ = try run("/usr/bin/curl", ["--fail", "--location", "--silent", "--show-error", "--proto", "=https", "--proto-redir", "=https", "--max-redirs", "5", "--max-filesize", String(maxCatalogBytes), "--output", catalogFile.path, catalogURL])
        _ = try run("/usr/bin/curl", ["--fail", "--location", "--silent", "--show-error", "--proto", "=https", "--proto-redir", "=https", "--max-redirs", "5", "--max-filesize", String(maxCatalogSignatureBytes), "--output", signatureFile.path, signatureURL])
        let catalogData = try limitedData(catalogFile, maximum: maxCatalogBytes)
        let signatureData = try limitedData(signatureFile, maximum: maxCatalogSignatureBytes)
        let catalog = try verify(catalogData, signatureData)
        try FileManager.default.createDirectory(at: cacheDirectory, withIntermediateDirectories: true)
        try catalogData.write(to: cachedCatalog, options: .atomic)
        try signatureData.write(to: cachedSignature, options: .atomic)
        return catalog
    }

    private static func verified(_ catalog: URL, _ signature: URL) throws -> Catalog {
        try verify(limitedData(catalog, maximum: maxCatalogBytes), limitedData(signature, maximum: maxCatalogSignatureBytes))
    }

    private static func verify(_ catalog: Data, _ signature: Data) throws -> Catalog {
        guard let pem = Bundle.main.url(forResource: "catalog-v2-public-key", withExtension: "pem") else {
            throw MyVibeError.invalid("The embedded catalog public key is missing.")
        }
        return try Safety.verifyCatalog(catalog, signatureData: signature, publicKeyPEM: String(contentsOf: pem, encoding: .utf8))
    }

    private static func limitedData(_ url: URL, maximum: Int) throws -> Data {
        let size = try url.resourceValues(forKeys: [.fileSizeKey]).fileSize ?? 0
        guard size > 0, size <= maximum else { throw MyVibeError.invalid("The catalog exceeds its safe size limit.") }
        return try Data(contentsOf: url, options: .mappedIfSafe)
    }
}

@discardableResult
func run(_ executable: String, _ arguments: [String]) throws -> String {
    let process = Process()
    process.executableURL = URL(fileURLWithPath: executable)
    process.arguments = arguments
    let pipe = Pipe()
    process.standardOutput = pipe
    process.standardError = pipe
    try process.run()
    let output = String(decoding: pipe.fileHandleForReading.readDataToEndOfFile(), as: UTF8.self)
    process.waitUntilExit()
    guard process.terminationStatus == 0 else { throw MyVibeError.invalid(output.trimmingCharacters(in: .whitespacesAndNewlines)) }
    return output
}

func shellQuote(_ value: String) -> String { "'" + value.replacingOccurrences(of: "'", with: "'\\''") + "'" }
func shellCommand(_ commands: [(String, [String])]) -> String {
    commands.map { ([$0.0] + $0.1).map(shellQuote).joined(separator: " ") }.joined(separator: " && ")
}
func appleQuote(_ value: String) -> String { "\"" + value.replacingOccurrences(of: "\\", with: "\\\\").replacingOccurrences(of: "\"", with: "\\\"") + "\"" }

extension ProcessInfo {
    var machineArchitecture: String {
        var info = utsname(); uname(&info)
        return withUnsafePointer(to: &info.machine) { pointer in
            pointer.withMemoryRebound(to: CChar.self, capacity: 1) { String(cString: $0) }
        }
    }
}

final class AppDelegate: NSObject, NSApplicationDelegate, NSTableViewDataSource, NSTableViewDelegate {
    private var window: NSWindow!
    private let table = NSTableView()
    private let status = NSTextField(labelWithString: "Checking for plug-in updates…")
    private let refreshButton = NSButton(title: "Check for updates", target: nil, action: nil)
    private let installButton = NSButton(title: "Install / Update", target: nil, action: nil)
    private let removeButton = NSButton(title: "Remove", target: nil, action: nil)
    private let versionPicker = NSPopUpButton(frame: .zero, pullsDown: false)
    private var catalog: Catalog?
    private var busy = false

    func applicationDidFinishLaunching(_ notification: Notification) {
        if let icon = Bundle.main.url(forResource: "MyVibe", withExtension: "icns") { NSApp.applicationIconImage = NSImage(contentsOf: icon) }
        window = NSWindow(contentRect: NSRect(x: 0, y: 0, width: 760, height: 520), styleMask: [.titled, .closable, .miniaturizable, .resizable], backing: .buffered, defer: false)
        window.title = "MyVibe \(managerVersion) beta"
        window.center()
        refreshButton.target = self; refreshButton.action = #selector(refreshCatalog)
        installButton.target = self; installButton.action = #selector(installSelected)
        removeButton.target = self; removeButton.action = #selector(removeSelected)
        let column = NSTableColumn(identifier: NSUserInterfaceItemIdentifier("plugin")); column.title = "Available plug-ins"; column.width = 680
        table.addTableColumn(column); table.delegate = self; table.dataSource = self; table.rowHeight = 44
        let scroll = NSScrollView(); scroll.documentView = table; scroll.hasVerticalScroller = true
        versionPicker.target = self; versionPicker.action = #selector(versionChanged)
        let actions = NSStackView(views: [versionPicker, installButton, removeButton]); actions.orientation = .horizontal; actions.spacing = 12
        let stack = NSStackView(views: [refreshButton, scroll, actions, status]); stack.orientation = .vertical; stack.spacing = 12; stack.edgeInsets = NSEdgeInsets(top: 20, left: 20, bottom: 20, right: 20)
        window.contentView = stack
        window.makeKeyAndOrderFront(nil)
        NSApp.activate(ignoringOtherApps: true)
        table.reloadData()
        table.selectRowIndexes(IndexSet(integer: 0), byExtendingSelection: false)
        updateVersionPicker()
        updateButtons()
        loadLocalCatalog()
        refreshCatalog()
    }

    func numberOfRows(in tableView: NSTableView) -> Int { Registry.products.count }
    func tableView(_ tableView: NSTableView, viewFor tableColumn: NSTableColumn?, row: Int) -> NSView? {
        let entry = Registry.products[row]
        let product = catalog?.plugins.first { $0.id == entry.id }
        let parts = Installer.installedParts(for: entry)
        let installedVersion = Installer.installedVersion(for: entry)
        let update = installedVersion.map { (product?.version ?? entry.version).compare($0, options: .numeric) == .orderedDescending } == true
        let installed = Installer.installationComplete(parts, for: entry) ? " — Installed \(installedVersion ?? "unknown")\(update ? " — Update available" : "")" : (parts.native || parts.cep ? " — Repair needed" : "")
        return NSTextField(labelWithString: "\(product?.name ?? entry.name) \(product?.version ?? entry.version)\(installed)\n\(product?.description ?? entry.description)")
    }

    func tableViewSelectionDidChange(_ notification: Notification) { updateVersionPicker(); updateButtons() }

    private func selectableProducts(for product: Product) -> [Product] {
        guard let releases = product.releases, !releases.isEmpty else { return [product] }
        return releases.map { product.selecting($0) }
    }

    private func selectedProduct() -> Product? {
        guard table.selectedRow >= 0,
              let product = catalog?.plugins.first(where: { $0.id == Registry.products[table.selectedRow].id }) else { return nil }
        let choices = selectableProducts(for: product)
        return choices.indices.contains(versionPicker.indexOfSelectedItem) ? choices[versionPicker.indexOfSelectedItem] : choices.first
    }

    private func updateVersionPicker() {
        versionPicker.removeAllItems()
        guard table.selectedRow >= 0,
              let product = catalog?.plugins.first(where: { $0.id == Registry.products[table.selectedRow].id }) else {
            versionPicker.addItem(withTitle: "No verified releases")
            versionPicker.isEnabled = false
            return
        }
        versionPicker.addItems(withTitles: selectableProducts(for: product).map { "Version \($0.version)" })
        versionPicker.selectItem(at: 0)
        versionPicker.isEnabled = !busy && versionPicker.numberOfItems > 1
    }

    @objc private func versionChanged() { updateButtons() }

    private func loadLocalCatalog() {
        if let cached = try? CatalogLoader.cached() {
            applyCatalog(cached, message: "Using last verified update data; checking for updates…")
            return
        }
        catalog = nil
        status.stringValue = "Plug-ins are available to manage. Install requires a verified catalog."
    }

    private func applyCatalog(_ verified: Catalog, message: String) {
        catalog = verified
        status.stringValue = message
        table.reloadData()
        updateVersionPicker()
        updateButtons()
    }

    @objc private func refreshCatalog() {
        guard !busy else { return }
        setBusy(true)
        status.stringValue = "Checking for verified plug-in updates…"
        Task { @MainActor in
            do {
                let fresh = try await Task.detached { try CatalogLoader.fetchAndCache() }.value
                applyCatalog(fresh, message: "Plug-in catalog is verified and up to date.")
            } catch {
                if catalog == nil { loadLocalCatalog() }
                status.stringValue = catalog == nil
                    ? "Plug-ins are listed, but installation is unavailable until a signed catalog is published."
                    : "Could not refresh; using the last verified catalog."
            }
            setBusy(false)
        }
    }

    @objc private func installSelected() {
        guard !busy else { return }
        guard table.selectedRow >= 0 else { status.stringValue = "Select a plug-in first."; return }
        let entry = Registry.products[table.selectedRow]
        guard let product = selectedProduct() else {
            status.stringValue = "\(entry.name) cannot be installed until its verified package is published."
            NSSound.beep()
            return
        }
        do {
            let artifact = try Safety.artifact(for: product)
            if !artifact.signature.required {
                let alert = NSAlert(); alert.messageText = "Unsigned beta package"; alert.informativeText = "MyVibe will still verify update data, official GitHub URL, and exact SHA-256. An unsigned CEP panel also requires Adobe CEP developer mode for this account. Install anyway?"; alert.addButton(withTitle: "Install Anyway"); alert.addButton(withTitle: "Cancel")
                guard alert.runModal() == .alertFirstButtonReturn else { return }
            }
            setBusy(true)
            status.stringValue = "Downloading and verifying \(product.name)…"
            Task { @MainActor in
                do {
                    let archive = try await Installer.download(artifact) { _ in }
                    try await Task.detached { try Installer.install(product: product, archive: archive) }.value
                    let previous = Installer.installedVersion(for: entry)
                    let verb = previous.map { product.version.compare($0, options: .numeric) == .orderedAscending ? "Rolled back" : "Installed" } ?? "Installed"
                    status.stringValue = "\(verb) \(product.name) \(product.version). Restart Illustrator."
                    table.reloadData(); updateButtons()
                } catch { show(error) }
                setBusy(false)
            }
        } catch { show(error) }
    }

    @objc private func removeSelected() {
        guard !busy else { return }
        guard table.selectedRow >= 0 else { status.stringValue = "Select a plug-in first."; return }
        let entry = Registry.products[table.selectedRow]
        let alert = NSAlert(); alert.messageText = "Remove \(entry.name)?"; alert.informativeText = "MyVibe will keep a local backup before removing the native plug-in and CEP interface."; alert.addButton(withTitle: "Remove"); alert.addButton(withTitle: "Cancel")
        guard alert.runModal() == .alertFirstButtonReturn else { return }
        setBusy(true)
        status.stringValue = "Backing up and removing \(entry.name)…"
        Task { @MainActor in
            do {
                try await Task.detached { try Installer.remove(entry: entry) }.value
                status.stringValue = "Removed \(entry.name). Restart Illustrator. A backup was kept."
                table.reloadData()
            } catch { show(error) }
            setBusy(false)
        }
    }

    private func updateButtons() {
        guard table.selectedRow >= 0 else { installButton.isEnabled = false; removeButton.isEnabled = false; return }
        let entry = Registry.products[table.selectedRow]
        let parts = Installer.installedParts(for: entry)
        let installedVersion = Installer.installedVersion(for: entry)
        let availableVersion = selectedProduct()?.version ?? catalog?.plugins.first { $0.id == entry.id }?.version ?? entry.version
        installButton.isEnabled = !busy
        installButton.title = Installer.installationComplete(parts, for: entry) ? (installedVersion.map { availableVersion.compare($0, options: .numeric) == .orderedDescending } == true ? "Update" : (installedVersion.map { availableVersion.compare($0, options: .numeric) == .orderedAscending } == true ? "Roll Back" : "Reinstall")) : (parts.native || parts.cep ? "Repair" : "Install")
        removeButton.isEnabled = !busy && (parts.native || parts.cep)
    }

    private func setBusy(_ value: Bool) {
        busy = value
        refreshButton.isEnabled = !value
        versionPicker.isEnabled = !value && versionPicker.numberOfItems > 1
        updateButtons()
    }

    private func show(_ error: Error) { status.stringValue = error.localizedDescription; NSSound.beep() }
}

func selfTest() throws {
    guard Safety.trustedReleaseURL("https://github.com/badrulmokhtar/myvibe/releases/download/test/file.zip"),
          !Safety.trustedReleaseURL("https://github.com/attacker/myvibe/releases/download/test/file.zip"),
          !Safety.trustedReleaseURL("http://github.com/badrulmokhtar/myvibe/releases/download/test/file.zip"),
          Registry.products.map(\.id) == ["com.badru.transform2d5", "com.badru.tonemesh", "com.badru.logolize"],
          Registry.product(id: "com.badru.transform2d5")?.nativeName == "2.5D Transform.aip",
          Registry.product(id: "com.badru.tonemesh")?.cepName == "ToneMesh",
          Registry.product(id: "com.badru.logolize")?.nativeName == nil,
          Registry.product(id: "com.badru.unknown") == nil,
          shellQuote("a'b") == "'a'\\''b'",
          shellCommand([("/bin/rm", ["-rf", "/tmp/a b"]), ("/bin/cp", ["-R", "/tmp/a b", "/tmp/c"])]) == "'/bin/rm' '-rf' '/tmp/a b' && '/bin/cp' '-R' '/tmp/a b' '/tmp/c'",
          let pem = Bundle.main.url(forResource: "catalog-public-key", withExtension: "pem"),
          let catalog = Bundle.main.url(forResource: "catalog-v1-test", withExtension: "json"),
          let signature = Bundle.main.url(forResource: "catalog-v1-test", withExtension: "json.sig"),
          Safety.verifySignature(try Data(contentsOf: catalog), signatureData: try Data(contentsOf: signature), publicKeyPEM: try String(contentsOf: pem, encoding: .utf8)) else {
        throw MyVibeError.invalid("Safety self-test failed.")
    }
    var changed = try Data(contentsOf: catalog); changed[0] ^= 1
    guard !Safety.verifySignature(changed, signatureData: try Data(contentsOf: signature), publicKeyPEM: try String(contentsOf: pem, encoding: .utf8)) else {
        throw MyVibeError.invalid("Tampered catalog self-test failed.")
    }
    print("MyVibe macOS self-test passed.")
}

@main
struct MyVibeApp {
    static func main() {
        if CommandLine.arguments.contains("--self-test") {
            do { try selfTest(); exit(0) } catch { fputs("\(error.localizedDescription)\n", stderr); exit(1) }
        }
        if CommandLine.arguments.count == 3, CommandLine.arguments[1] == "--preflight-archive" {
            do { _ = try Safety.preflightArchive(URL(fileURLWithPath: CommandLine.arguments[2])); print("Archive preflight passed."); exit(0) }
            catch { fputs("\(error.localizedDescription)\n", stderr); exit(1) }
        }
        let app = NSApplication.shared
        let delegate = AppDelegate()
        app.delegate = delegate
        app.setActivationPolicy(.regular)
        app.run()
    }
}
