using System;
using Xunit;

namespace MainService.Tests.UseCases
{
    public class SimpleTest
    {
        [Fact]
        public void SimpleTest_ShouldPass()
        {
            var expected = 2;
            var actual = 1 + 1;
            Assert.Equal(expected, actual);
        }
    }
}