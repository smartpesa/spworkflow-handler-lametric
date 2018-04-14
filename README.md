# spWorkflow Handler: LaMetric

[![license](https://img.shields.io/github/license/cosmos/cosmos-sdk.svg)](https://github.com/smartpesa/spworkflow-handler-lametric/master/LICENSE)

Template of handler dynamically loaded by spWorkflow using reflection of precompiled library. Spring and config match the assembly name and dynamically propogate json messages to this handler.

This template can be used for data provider, post processing, and subscriber real-time event handling. Note:
  1. subscriber receives Payment class (with sub-classes of type card, qr and crypto payment)
  2. data provider receives DataProvider class with extra_data dictionary
