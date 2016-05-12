var mkdirp = require('mkdirp');
var path = require('path');
var ncp = require('ncp');

// Paths
var src1 = path.join(__dirname, '..', 'assets');
var dir1 = path.join(__dirname, '..', '..', '..', 'Assets', 'packages');

var src2 = path.join(__dirname, '..', 'roslynsupport');
var dir2 = path.join(__dirname, '..', '..', '..', 'RoslynSupport');

// Create folder if missing
mkdirp(dir1, function (err) {
  if (err) {
    console.error(err)
    process.exit(1);
  }

  // Copy files
  ncp(src1, dir1, function (err) {
    if (err) {
      console.error(err);
      process.exit(1);
    }
  });
});

// Create folder if missing
mkdirp(dir2, function (err) {
    if (err) {
        console.error(err)
        process.exit(1);
    }

    // Copy files
    ncp(src2, dir2, function (err) {
        if (err) {
            console.error(err);
            process.exit(1);
        }
    });
});