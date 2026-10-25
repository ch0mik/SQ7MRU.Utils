import AppKit

// Render source artwork on the macOS runner; no generated icons enter Git.
guard CommandLine.arguments.count == 2 else { fatalError("Expected output PNG path") }
let size = 1024
guard let bitmap = NSBitmapImageRep(bitmapDataPlanes: nil, pixelsWide: size, pixelsHigh: size,
    bitsPerSample: 8, samplesPerPixel: 4, hasAlpha: true, isPlanar: false,
    colorSpaceName: .deviceRGB, bytesPerRow: 0, bitsPerPixel: 0),
    let context = NSGraphicsContext(bitmapImageRep: bitmap) else { fatalError("Cannot create icon") }
NSGraphicsContext.saveGraphicsState()
NSGraphicsContext.current = context
NSColor.clear.setFill()
NSRect(x: 0, y: 0, width: CGFloat(size), height: CGFloat(size)).fill()
NSColor(calibratedRed: 0.08, green: 0.17, blue: 0.28, alpha: 1).setFill()
NSBezierPath(roundedRect: NSRect(x: 40, y: 40, width: 944, height: 944), xRadius: 200, yRadius: 200).fill()
let title = "QSL" as NSString
let attributes: [NSAttributedString.Key: Any] = [
    .font: NSFont.boldSystemFont(ofSize: 240), .foregroundColor: NSColor.white
]
let textSize = title.size(withAttributes: attributes)
title.draw(at: NSPoint(x: (1024 - textSize.width) / 2, y: 490), withAttributes: attributes)
NSColor(calibratedRed: 0.1, green: 0.7, blue: 0.95, alpha: 1).setFill()
let arrow = NSBezierPath()
arrow.move(to: NSPoint(x: 452, y: 440))
arrow.line(to: NSPoint(x: 572, y: 440))
arrow.line(to: NSPoint(x: 572, y: 330))
arrow.line(to: NSPoint(x: 680, y: 330))
arrow.line(to: NSPoint(x: 512, y: 170))
arrow.line(to: NSPoint(x: 344, y: 330))
arrow.line(to: NSPoint(x: 452, y: 330))
arrow.close()
arrow.fill()
NSGraphicsContext.restoreGraphicsState()
guard let data = bitmap.representation(using: .png, properties: [:]) else { fatalError("Cannot encode icon") }
try data.write(to: URL(fileURLWithPath: CommandLine.arguments[1]))
