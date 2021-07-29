//Copyright(c) 2021 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/MultiFactor.Ldap.Adapter/blob/main/LICENSE.md

//MIT License
//Copyright(c) 2017 Verner Fortelius

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

namespace MultiFactor.Ldap.Adapter.Core
{
    /// <summary>
    /// https://tools.ietf.org/html/rfc4511
    /// </summary>
    public enum LdapResult
    {
        success = 0,
        operationError = 1,
        protocolError = 2,
        timeLimitExceeded = 3,
        sizeLimitExceeded = 4,
        compareFalse = 5,
        compareTrue = 6,
        authMethodNotSupported = 7,
        strongerAuthRequired = 8,
        // 9 reserved --
        referral = 10,
        adminLimitExceeded = 11,
        unavailableCriticalExtension = 12,
        confidentialityRequired = 13,
        saslBindInProgress = 14,
        noSuchAttribute = 16,
        undefinedAttributeType = 17,
        inappropriateMatching = 18,
        constraintViolation = 19,
        attributeOrValueExists = 20,
        invalidAttributeSyntax = 21,
        // 22-31 unused --
        noSuchObject = 32,
        aliasProblem = 33,
        invalidDNSyntax = 34,
        // 35 reserved for undefined isLeaf --
        aliasDereferencingProblem = 36,
        // 37-47 unused --
        inappropriateAuthentication = 48,
        invalidCredentials = 49,
        insufficientAccessRights = 50,
        busy = 51,
        unavailable = 52,
        unwillingToPerform = 53,
        loopDetect = 54,
        // 55-63 unused --
        namingViolation = 64,
        objectClassViolation = 65,
        notAllowedOnNonLeaf = 66,
        notAllowedOnRDN = 67,
        entryAlreadyExists = 68,
        objectClassModsProhibited = 69,
        // 70 reserved for CLDAP --
        affectsMultipleDSAs = 71,
        // 72-79 unused --
        other = 80,
    }
}
