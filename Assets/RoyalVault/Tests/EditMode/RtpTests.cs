using NUnit.Framework;
using RoyalVault.Core;
using UnityEngine;

namespace RoyalVault.Tests
{
    // Each shipped machine must keep its RTP in the target band
    public sealed class RtpTests
    {
        [Test]
        public void RoyalVault_ReturnsAbout95Percent()
        {
            RtpReport report = RtpSimulator.Run(MachinePresets.RoyalVault5x3(), 1000000, 1);
            Debug.Log("Royal Vault 5x3: " + report);
            Assert.That(report.Rtp, Is.InRange(93.0, 98.0));
            Assert.That(report.HitRate, Is.InRange(20.0, 35.0));
        }

        [Test]
        public void Classic3x3_ReturnsAbout96Percent()
        {
            RtpReport report = RtpSimulator.Run(MachinePresets.Classic3x3(), 1000000, 1);
            Debug.Log("Classic 3x3: " + report);
            Assert.That(report.Rtp, Is.InRange(93.0, 98.5));
        }
    }
}
